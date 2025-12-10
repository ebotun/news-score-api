using NewsScoreApi.Data;
using NewsScoreApi.DTOs;
using NewsScoreApi.Models;
using NewsScoreApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NewsScoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsScoreController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly INewsScoreService _newsScoreService;
    private readonly ILogger<NewsScoreController> _logger;

    public NewsScoreController(ApplicationDbContext context, INewsScoreService newsScoreService, ILogger<NewsScoreController> logger)
    {
        _context = context;
        _newsScoreService = newsScoreService;
        _logger = logger;
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<NewsScoreResponseDto>> CalculateScore([FromBody] NewsScoreRequestDto request)
    {
        if (request.Measurements == null || request.Measurements.Count == 0)
        {
            _logger.LogWarning("Calculate score request received with no measurements");
            return BadRequest("Measurements are required");
        }

        var requiredTypes = Enum.GetValues<MeasurementType>()
            .Select(e => e.ToString().ToUpperInvariant())
            .ToArray();
        var providedTypes = request.Measurements.Select(m => m.Type.ToUpperInvariant()).ToList();

        if (!requiredTypes.All(type => providedTypes.Contains(type)))
        {
            var missingTypes = string.Join(", ", requiredTypes);
            _logger.LogWarning("Calculate score request missing required measurement types. Missing: {MissingTypes}", missingTypes);
            return BadRequest($"All measurement types ({missingTypes}) are required");
        }

        try
        {
            var (totalScore, validationErrors) = await _newsScoreService.CalculateScoreAsync(request.Measurements);

            if (validationErrors.Count != 0)
            {
                _logger.LogWarning("Score calculation failed with {ErrorCount} validation errors", validationErrors.Count);
                return BadRequest(new ValidationErrorsResponseDto { Errors = validationErrors });
            }

            return Ok(new NewsScoreResponseDto { Score = totalScore });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating NEWS score");
            return StatusCode(500, "An error occurred while calculating the score");
        }
    }

    [HttpGet("ranges")]
    public async Task<ActionResult<List<NewsScoreRangeDto>>> GetRanges([FromQuery] string? measurementType = null)
    {
        var query = _context.NewsScoreRanges.AsQueryable();

        if (!string.IsNullOrEmpty(measurementType)) query = query.Where(r => r.MeasurementType == measurementType);

        var ranges = await query
            .OrderBy(r => r.MeasurementType)
            .ThenBy(r => r.MinValue)
            .Select(r => new NewsScoreRangeDto
            {
                Id = r.Id,
                MeasurementType = r.MeasurementType,
                MinValue = r.MinValue,
                MaxValue = r.MaxValue,
                Score = r.Score
            })
            .ToListAsync();

        return Ok(ranges);
    }

    [HttpPost("ranges")]
    public async Task<ActionResult> CreateRanges([FromBody] CreateRangesRequestDto request)
    {
        if (request.Ranges == null || request.Ranges.Count == 0)
        {
            _logger.LogWarning("Create ranges request received with no ranges");
            return BadRequest("At least one range is required");
        }

        var errors = new List<string>();

        foreach (var rangeDto in request.Ranges)
        {
            if (rangeDto.MinValue >= rangeDto.MaxValue)
            {
                errors.Add(
                    $"Range for {rangeDto.MeasurementType}: MinValue ({rangeDto.MinValue}) must be less than MaxValue ({rangeDto.MaxValue})");
                continue;
            }

            if (string.IsNullOrWhiteSpace(rangeDto.MeasurementType))
            {
                errors.Add("MeasurementType is required for all ranges");
                continue;
            }

            var existingRanges = await _context.NewsScoreRanges
                .Where(r => r.MeasurementType == rangeDto.MeasurementType &&
                            (rangeDto.Id == null || r.Id != rangeDto.Id.Value))
                .ToListAsync();

            foreach (var existingRange in existingRanges)
            {
                if (RangesOverlap(rangeDto.MinValue, rangeDto.MaxValue, existingRange.MinValue, existingRange.MaxValue))
                {
                    errors.Add($"Range for {rangeDto.MeasurementType} ({rangeDto.MinValue}, {rangeDto.MaxValue}] overlaps with existing range ({existingRange.MinValue}, {existingRange.MaxValue}]");
                }
            }
        }

        if (errors.Count != 0) return BadRequest(new { errors });

        var newRanges = request.Ranges.Select(r => new NewsScoreRange
        {
            MeasurementType = r.MeasurementType,
            MinValue = r.MinValue,
            MaxValue = r.MaxValue,
            Score = r.Score
        }).ToList();

        await _context.NewsScoreRanges.AddRangeAsync(newRanges);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created {Count} new score ranges", newRanges.Count);

        return CreatedAtAction(nameof(GetRanges), new { measurementType = (string?)null }, newRanges.Select(r =>
            new NewsScoreRangeDto
            {
                Id = r.Id,
                MeasurementType = r.MeasurementType,
                MinValue = r.MinValue,
                MaxValue = r.MaxValue,
                Score = r.Score
            }).ToList());
    }

    [HttpPut("ranges/{id}")]
    public async Task<ActionResult<NewsScoreRangeDto>> UpdateRange(int id, [FromBody] NewsScoreRangeDto rangeDto)
    {
        var range = await _context.NewsScoreRanges.FindAsync(id);
        if (range == null)
        {
            _logger.LogWarning("Update range request for non-existent range id: {RangeId}", id);
            return NotFound($"Range with id {id} not found");
        }

        if (rangeDto.MinValue >= rangeDto.MaxValue)
            return BadRequest($"MinValue ({rangeDto.MinValue}) must be less than MaxValue ({rangeDto.MaxValue})");

        var existingRanges = await _context.NewsScoreRanges
            .Where(r => r.MeasurementType == rangeDto.MeasurementType && r.Id != id)
            .ToListAsync();

        foreach (var existingRange in existingRanges)
            if (RangesOverlap(rangeDto.MinValue, rangeDto.MaxValue, existingRange.MinValue, existingRange.MaxValue))
                return BadRequest(
                    $"Range ({rangeDto.MinValue}, {rangeDto.MaxValue}] overlaps with existing range ({existingRange.MinValue}, {existingRange.MaxValue}]");

        range.MeasurementType = rangeDto.MeasurementType;
        range.MinValue = rangeDto.MinValue;
        range.MaxValue = rangeDto.MaxValue;
        range.Score = rangeDto.Score;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated range id: {RangeId}, type: {MeasurementType}", id, rangeDto.MeasurementType);

        return Ok(new NewsScoreRangeDto
        {
            Id = range.Id,
            MeasurementType = range.MeasurementType,
            MinValue = range.MinValue,
            MaxValue = range.MaxValue,
            Score = range.Score
        });
    }

    [HttpDelete("ranges/{id}")]
    public async Task<ActionResult> DeleteRange(int id)
    {
        var range = await _context.NewsScoreRanges.FindAsync(id);
        if (range == null)
        {
            _logger.LogWarning("Delete range request for non-existent range id: {RangeId}", id);
            return NotFound($"Range with id {id} not found");
        }

        _context.NewsScoreRanges.Remove(range);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted range id: {RangeId}, type: {MeasurementType}", id, range.MeasurementType);

        return NoContent();
    }

    [HttpDelete("ranges")]
    public async Task<ActionResult> DeleteRanges([FromBody] List<int>? ids)
    {
        if (ids == null || ids.Count == 0) return BadRequest("At least one id is required");

        var ranges = await _context.NewsScoreRanges
            .Where(r => ids.Contains(r.Id))
            .ToListAsync();

        if (ranges.Count != ids.Count)
        {
            var foundIds = ranges.Select(r => r.Id).ToList();
            var notFoundIds = ids.Except(foundIds).ToList();
            return NotFound($"Ranges with ids {string.Join(", ", notFoundIds)} not found");
        }

        _context.NewsScoreRanges.RemoveRange(ranges);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static bool RangesOverlap(decimal min1, decimal max1, decimal min2, decimal max2)
    {
        return min1 < max2 && max1 > min2;
    }
}