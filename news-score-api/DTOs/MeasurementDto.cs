using System.ComponentModel.DataAnnotations;

namespace NewsScoreApi.DTOs;

public class MeasurementDto
{
    [Required]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    public decimal Value { get; set; }
}

