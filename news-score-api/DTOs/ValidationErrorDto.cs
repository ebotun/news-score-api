namespace NewsScoreApi.DTOs;

public class RangeInfoDto
{
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
}

public class ValidationErrorDto
{
    public string Error { get; set; } = string.Empty;
    public string MeasurementType { get; set; } = string.Empty;
    public decimal InvalidValue { get; set; }
    public List<RangeInfoDto> AvailableRanges { get; set; } = [];
}

public class ValidationErrorsResponseDto
{
    public List<ValidationErrorDto> Errors { get; set; } = [];
}

