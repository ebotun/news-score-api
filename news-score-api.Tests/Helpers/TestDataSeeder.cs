using Microsoft.EntityFrameworkCore;
using NewsScoreApi.Data;
using NewsScoreApi.Models;

namespace news_score_api.Tests.Helpers;

public static class TestDataSeeder
{
    public static void SeedStandardRanges(ApplicationDbContext context)
    {
        if (context.NewsScoreRanges.Any())
            return;

        var ranges = new List<NewsScoreRange>
        {
            new() { MeasurementType = "TEMP", MinValue = 31, MaxValue = 35, Score = 3 },
            new() { MeasurementType = "TEMP", MinValue = 35, MaxValue = 36, Score = 1 },
            new() { MeasurementType = "TEMP", MinValue = 36, MaxValue = 38, Score = 0 },
            new() { MeasurementType = "TEMP", MinValue = 38, MaxValue = 39, Score = 1 },
            new() { MeasurementType = "TEMP", MinValue = 39, MaxValue = 42, Score = 2 },

            new() { MeasurementType = "HR", MinValue = 25, MaxValue = 40, Score = 3 },
            new() { MeasurementType = "HR", MinValue = 40, MaxValue = 50, Score = 1 },
            new() { MeasurementType = "HR", MinValue = 50, MaxValue = 90, Score = 0 },
            new() { MeasurementType = "HR", MinValue = 90, MaxValue = 110, Score = 1 },
            new() { MeasurementType = "HR", MinValue = 110, MaxValue = 130, Score = 2 },
            new() { MeasurementType = "HR", MinValue = 130, MaxValue = 220, Score = 3 },

            new() { MeasurementType = "RR", MinValue = 3, MaxValue = 8, Score = 3 },
            new() { MeasurementType = "RR", MinValue = 8, MaxValue = 11, Score = 1 },
            new() { MeasurementType = "RR", MinValue = 11, MaxValue = 20, Score = 0 },
            new() { MeasurementType = "RR", MinValue = 20, MaxValue = 24, Score = 2 },
            new() { MeasurementType = "RR", MinValue = 24, MaxValue = 60, Score = 3 }
        };

        context.NewsScoreRanges.AddRange(ranges);
        context.SaveChanges();
    }
}

