namespace NewsScoreApi.DTOs;

public class NewsScoreRangeDto
{
    public int? Id { get; set; }
    public string MeasurementType { get; set; } = string.Empty;
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public int Score { get; set; }
}

