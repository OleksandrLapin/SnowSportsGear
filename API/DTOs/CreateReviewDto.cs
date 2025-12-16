using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class CreateReviewDto
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(150)]
    public string? Title { get; set; }

    [MaxLength(1000)]
    public string? Content { get; set; }

    [Range(1, int.MaxValue)]
    public int? OrderId { get; set; }
}
