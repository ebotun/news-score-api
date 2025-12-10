using System.ComponentModel.DataAnnotations;

namespace NewsScoreApi.DTOs;

public class NewsScoreRequestDto
{
    [Required]
    public List<MeasurementDto>? Measurements { get; set; } = [];
}

