namespace API.DTOs;

public class ReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Author { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public string? AdminResponse { get; set; }
    public string? AdminResponder { get; set; }
    public DateTime? AdminRespondedAt { get; set; }
}
