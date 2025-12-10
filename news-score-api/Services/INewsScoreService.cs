using NewsScoreApi.DTOs;

namespace NewsScoreApi.Services;

public interface INewsScoreService
{
    Task<(int Score, List<ValidationErrorDto> ValidationErrors)> CalculateScoreAsync(
        List<MeasurementDto> measurements,
        CancellationToken cancellationToken = default);
}

