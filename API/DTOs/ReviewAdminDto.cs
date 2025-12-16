namespace API.DTOs;

public class ReviewAdminDto : ReviewDto
{
    public bool IsHidden { get; set; }
    public string AuthorEmail { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string AdminResponderEmail { get; set; } = string.Empty;
    public int? OrderId { get; set; }
}
