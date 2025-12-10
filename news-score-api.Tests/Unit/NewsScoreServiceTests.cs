using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NewsScoreApi.Data;
using NewsScoreApi.DTOs;
using NewsScoreApi.Services;
using news_score_api.Tests.Helpers;

namespace news_score_api.Tests.Unit;

public class NewsScoreServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly NewsScoreService _service;
    private readonly Mock<ILogger<NewsScoreService>> _loggerMock;

    public NewsScoreServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<NewsScoreService>>();
        _service = new NewsScoreService(_context, _loggerMock.Object);
        
        TestDataSeeder.SeedStandardRanges(_context);
    }

    [Fact]
    public async Task CalculateScoreAsync_WithValidMeasurements_ReturnsCorrectScore()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "TEMP", Value = 37 }, 
            new() { Type = "HR", Value = 60 },   
            new() { Type = "RR", Value = 5 }    
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        score.Should().Be(3);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateScoreAsync_WithAllZeroScores_ReturnsZero()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "TEMP", Value = 37 },
            new() { Type = "HR", Value = 75 },
            new() { Type = "RR", Value = 15 }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        score.Should().Be(0);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateScoreAsync_WithHighScores_ReturnsSum()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "TEMP", Value = 33 },
            new() { Type = "HR", Value = 30 },
            new() { Type = "RR", Value = 5 }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        score.Should().Be(9);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateScoreAsync_WithBoundaryValues_HandlesExclusiveMinAndInclusiveMax()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "TEMP", Value = 35.01m },
            new() { Type = "TEMP", Value = 36 },
            new() { Type = "TEMP", Value = 36.01m }
        };

        var (score1, _) = await _service.CalculateScoreAsync(new List<MeasurementDto> { measurements[0] });
        var (score2, _) = await _service.CalculateScoreAsync(new List<MeasurementDto> { measurements[1] });
        var (score3, _) = await _service.CalculateScoreAsync(new List<MeasurementDto> { measurements[2] });

        score1.Should().Be(1);
        score2.Should().Be(1);
        score3.Should().Be(0);
    }

    [Fact]
    public async Task CalculateScoreAsync_WithValueAtExclusiveMin_ReturnsValidationError()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "TEMP", Value = 31 }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        score.Should().Be(0);
        errors.Should().HaveCount(1);
        errors[0].MeasurementType.Should().Be("TEMP");
        errors[0].InvalidValue.Should().Be(31);
        errors[0].AvailableRanges.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CalculateScoreAsync_WithValueOutsideRanges_ReturnsValidationError()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "TEMP", Value = 50 }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        score.Should().Be(0);
        errors.Should().HaveCount(1);
        errors[0].MeasurementType.Should().Be("TEMP");
        errors[0].InvalidValue.Should().Be(50);
        errors[0].AvailableRanges.Should().NotBeEmpty();
        errors[0].Error.Should().Contain("outside defined ranges");
    }

    [Fact]
    public async Task CalculateScoreAsync_WithMultipleInvalidValues_ReturnsMultipleErrors()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "TEMP", Value = 50 },
            new() { Type = "HR", Value = 5 }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        score.Should().Be(0);
        errors.Should().HaveCount(2);
        errors.Should().Contain(e => e.MeasurementType == "TEMP");
        errors.Should().Contain(e => e.MeasurementType == "HR");
    }

    [Fact]
    public async Task CalculateScoreAsync_WithCaseInsensitiveType_WorksCorrectly()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "temp", Value = 37 },
            new() { Type = "Hr", Value = 60 },
            new() { Type = "rr", Value = 15 }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        score.Should().Be(0);
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateScoreAsync_WithMixedValidAndInvalid_ReturnsPartialScore()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "TEMP", Value = 37 },
            new() { Type = "HR", Value = 5 }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        score.Should().Be(0);
        errors.Should().HaveCount(1);
        errors[0].MeasurementType.Should().Be("HR");
    }

    [Fact]
    public async Task CalculateScoreAsync_WithDecimalHR_ReturnsValidationError()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "HR", Value = 60.5m }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        score.Should().Be(0);
        errors.Should().HaveCount(1);
        errors[0].MeasurementType.Should().Be("HR");
        errors[0].InvalidValue.Should().Be(60.5m);
        errors[0].Error.Should().Contain("must be a whole number");
        errors[0].Error.Should().Contain("integer");
    }

    [Fact]
    public async Task CalculateScoreAsync_WithDecimalRR_ReturnsValidationError()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "RR", Value = 15.7m }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        score.Should().Be(0);
        errors.Should().HaveCount(1);
        errors[0].MeasurementType.Should().Be("RR");
        errors[0].InvalidValue.Should().Be(15.7m);
        errors[0].Error.Should().Contain("must be a whole number");
        errors[0].Error.Should().Contain("integer");
    }

    [Fact]
    public async Task CalculateScoreAsync_WithDecimalTEMP_WorksCorrectly()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "TEMP", Value = 37.5m }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        errors.Should().BeEmpty();
        score.Should().Be(0);
    }

    [Fact]
    public async Task CalculateScoreAsync_WithWholeNumberHR_WorksCorrectly()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "HR", Value = 60 }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        errors.Should().BeEmpty();
        score.Should().Be(0);
    }

    [Fact]
    public async Task CalculateScoreAsync_WithWholeNumberRR_WorksCorrectly()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "RR", Value = 15 }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        errors.Should().BeEmpty();
        score.Should().Be(0);
    }

    [Fact]
    public async Task CalculateScoreAsync_WithMultipleDecimalHRRR_ReturnsMultipleErrors()
    {
        var measurements = new List<MeasurementDto>
        {
            new() { Type = "HR", Value = 60.5m },
            new() { Type = "RR", Value = 15.3m }
        };

        var (score, errors) = await _service.CalculateScoreAsync(measurements);

        score.Should().Be(0);
        errors.Should().HaveCount(2);
        errors.Should().Contain(e => e.MeasurementType == "HR" && e.Error.Contains("whole number"));
        errors.Should().Contain(e => e.MeasurementType == "RR" && e.Error.Contains("whole number"));
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

