using API.DTOs;
using Core.Entities;

namespace API.Extensions;

public static class ReviewMappingExtensions
{
    public static ReviewDto ToDto(this ProductReview review, string? currentUserId = null)
    {
        return new ReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            Rating = review.Rating,
            Title = review.Title,
            Content = review.Content,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt,
            Author = GetAuthor(review.User),
            IsOwner = !string.IsNullOrEmpty(currentUserId) && review.UserId == currentUserId,
            AdminResponse = review.AdminResponse,
            AdminResponder = review.AdminResponderEmail,
            AdminRespondedAt = review.AdminRespondedAt
        };
    }

    public static ReviewAdminDto ToAdminDto(this ProductReview review, string? currentUserId = null)
    {
        var dto = review.ToDto(currentUserId);
        return new ReviewAdminDto
        {
            Id = dto.Id,
            ProductId = dto.ProductId,
            Rating = dto.Rating,
            Title = dto.Title,
            Content = dto.Content,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            Author = dto.Author,
            IsOwner = dto.IsOwner,
            IsHidden = review.IsHidden,
            AuthorEmail = review.User.Email ?? string.Empty,
            ProductName = review.Product.Name,
            Brand = review.Product.Brand,
            Type = review.Product.Type,
            AdminResponderEmail = review.AdminResponderEmail ?? string.Empty,
            OrderId = review.OrderId
        };
    }

    public static ReviewAuditDto ToDto(this ReviewAudit audit)
    {
        return new ReviewAuditDto
        {
            Id = audit.Id,
            ReviewId = audit.ReviewId,
            Action = audit.Action,
            ActorEmail = audit.ActorEmail,
            CreatedAt = audit.CreatedAt,
            OldRating = audit.OldRating,
            NewRating = audit.NewRating,
            OldTitle = audit.OldTitle,
            NewTitle = audit.NewTitle,
            OldContent = audit.OldContent,
            NewContent = audit.NewContent,
            OldHidden = audit.OldHidden,
            NewHidden = audit.NewHidden,
            OldResponse = audit.OldResponse,
            NewResponse = audit.NewResponse
        };
    }

    private static string GetAuthor(AppUser user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(name)) return name;
        return user.Email ?? "User";
    }
}
