using Core.Entities;

namespace API.DTOs;

public class ReviewAuditDto
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public ReviewAction Action { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? OldRating { get; set; }
    public int? NewRating { get; set; }
    public string? OldTitle { get; set; }
    public string? NewTitle { get; set; }
    public string? OldContent { get; set; }
    public string? NewContent { get; set; }
    public bool? OldHidden { get; set; }
    public bool? NewHidden { get; set; }
    public string? OldResponse { get; set; }
    public string? NewResponse { get; set; }
}
