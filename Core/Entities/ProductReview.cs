using System.ComponentModel.DataAnnotations;
using Core.Entities.OrderAggregate;

namespace Core.Entities;

public class ProductReview : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(150)]
    public string? Title { get; set; }

    [MaxLength(1000)]
    public string? Content { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsHidden { get; set; }
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    [MaxLength(1000)]
    public string? AdminResponse { get; set; }
    [MaxLength(450)]
    public string? AdminResponderId { get; set; }
    [MaxLength(256)]
    public string? AdminResponderEmail { get; set; }
    public DateTime? AdminRespondedAt { get; set; }
}
