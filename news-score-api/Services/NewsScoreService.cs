using Microsoft.EntityFrameworkCore;
using NewsScoreApi.Data;
using NewsScoreApi.DTOs;
using NewsScoreApi.Models;

namespace NewsScoreApi.Services;

public class NewsScoreService : INewsScoreService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NewsScoreService> _logger;

    public NewsScoreService(ApplicationDbContext context, ILogger<NewsScoreService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(int Score, List<ValidationErrorDto> ValidationErrors)> CalculateScoreAsync(
        List<MeasurementDto> measurements,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = new List<ValidationErrorDto>();
        var totalScore = 0;

        foreach (var measurement in measurements)
        {
            var caseInsensitiveType = measurement.Type.ToUpperInvariant();
            
            if (caseInsensitiveType == "HR" || caseInsensitiveType == "RR")
            {
                if (measurement.Value % 1 != 0)
                {
                    _logger.LogWarning(
                        "Invalid decimal value for {MeasurementType}: {Value}. Must be a whole number.",
                        measurement.Type, measurement.Value);
                    
                    validationErrors.Add(new ValidationErrorDto
                    {
                        Error = $"Invalid value {measurement.Value} for measurement type {measurement.Type}. {measurement.Type} must be a whole number (integer).",
                        MeasurementType = measurement.Type,
                        InvalidValue = measurement.Value,
                        AvailableRanges = new List<RangeInfoDto>()
                    });
                    continue;
                }
            }
            
            var range = await _context.NewsScoreRanges
                .FirstOrDefaultAsync(r =>
                    r.MeasurementType == caseInsensitiveType &&
                    measurement.Value > r.MinValue &&
                    measurement.Value <= r.MaxValue,
                    cancellationToken);

            if (range == null)
            {
                _logger.LogWarning(
                    "Value {Value} for {MeasurementType} is outside defined ranges.",
                    measurement.Value, measurement.Type);
                
                var availableRanges = await _context.NewsScoreRanges
                    .Where(r => r.MeasurementType == caseInsensitiveType)
                    .OrderBy(r => r.MinValue)
                    .Select(r => new RangeInfoDto
                    {
                        MinValue = r.MinValue,
                        MaxValue = r.MaxValue
                    })
                    .ToListAsync(cancellationToken);

                validationErrors.Add(new ValidationErrorDto
                {
                    Error =
                        $"Invalid value {measurement.Value} for measurement type {measurement.Type}. Value is outside defined ranges.",
                    MeasurementType = measurement.Type,
                    InvalidValue = measurement.Value,
                    AvailableRanges = availableRanges
                });
            }
            else
            {
                totalScore += range.Score;
            }
        }

        if (validationErrors.Count == 0)
        {
            _logger.LogInformation(
                "Successfully calculated NEWS score: {Score} for {MeasurementCount} measurements",
                totalScore, measurements.Count);
        }

        return (totalScore, validationErrors);
    }
}

