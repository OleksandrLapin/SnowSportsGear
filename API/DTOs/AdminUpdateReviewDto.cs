using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class AdminUpdateReviewDto
{
    [Range(1,5)]
    public int Rating { get; set; }

    [MaxLength(150)]
    public string? Title { get; set; }

    [MaxLength(1000)]
    public string? Content { get; set; }

    public bool IsHidden { get; set; }
}
