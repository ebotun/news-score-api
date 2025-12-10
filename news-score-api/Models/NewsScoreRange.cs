using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewsScoreApi.Models;

[Table("NewsScoreRanges")]
public class NewsScoreRange
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string MeasurementType { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal MinValue { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxValue { get; set; }

    [Required]
    public int Score { get; set; }
}

