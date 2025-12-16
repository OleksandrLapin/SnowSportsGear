using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class ReviewReplyDto
{
    [MaxLength(1000)]
    public string? Response { get; set; }
}

