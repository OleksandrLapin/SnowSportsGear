using API.DTOs;
using API.Extensions;
using API.RequestHelpers;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class ReviewsController(IReviewRepository reviewRepo, IProductRepository productRepo, UserManager<AppUser> userManager) : BaseApiController
{
    [AllowAnonymous]
    [HttpGet("/api/products/{productId:int}/reviews")]
    public async Task<ActionResult<Pagination<ReviewDto>>> GetProductReviews(int productId, [FromQuery] ReviewSpecParams specParams)
    {
        specParams.ProductId = productId;
        var currentUserId = await GetCurrentUserIdAsync();
        var (data, count) = await reviewRepo.GetProductReviewsAsync(specParams);

        var reviews = data.Select(r => r.ToDto(currentUserId)).ToList();
        var pagination = new Pagination<ReviewDto>(specParams.PageIndex, specParams.PageSize, count, reviews);
        return Ok(pagination);
    }

    [HttpGet("/api/products/{productId:int}/reviews/me")]
    public async Task<ActionResult<ReviewDto>> GetMyReview(int productId)
    {
        var user = await userManager.GetUserByEmail(User);
        var review = await reviewRepo.GetUserReviewForProductAsync(user.Id, productId);
        if (review == null) return NoContent();
        return Ok(review.ToDto(user.Id));
    }

    [HttpGet("/api/products/{productId:int}/reviews/eligibility")]
    public async Task<ActionResult<ReviewEligibilityDto>> GetEligibility(int productId)
    {
        var user = await userManager.GetUserByEmail(User);
        var existing = await reviewRepo.GetUserReviewForProductAsync(user.Id, productId);
        var hasPurchased = await reviewRepo.HasUserPurchasedProductAsync(user.Email ?? string.Empty, productId);

        return new ReviewEligibilityDto
        {
            CanReview = hasPurchased && existing == null,
            AlreadyReviewed = existing != null
        };
    }

    [InvalidateCache("api/products|")]
    [HttpPost("/api/products/{productId:int}/reviews")]
    public async Task<ActionResult<ReviewDto>> CreateReview(int productId, [FromBody] CreateReviewDto dto)
    {
        var user = await userManager.GetUserByEmail(User);

        if (!productRepo.ProductExists(productId))
        {
            return NotFound("Product not found");
        }

        var hasPurchased = await reviewRepo.HasUserPurchasedProductAsync(user.Email ?? string.Empty, productId);
        if (!hasPurchased)
        {
            return BadRequest("You can only review products you purchased");
        }

        var existing = await reviewRepo.GetUserReviewForProductAsync(user.Id, productId);
        if (existing != null) return BadRequest("You already reviewed this product");

        var now = DateTime.UtcNow;
        var orderId = await reviewRepo.GetOrderIdForUserProductAsync(user.Email ?? string.Empty, productId, dto.OrderId);
        if (dto.OrderId.HasValue && orderId == null) return BadRequest("Order not found for this product");

        var review = new ProductReview
        {
            ProductId = productId,
            UserId = user.Id,
            User = user,
            Rating = dto.Rating,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title.Trim(),
            Content = string.IsNullOrWhiteSpace(dto.Content) ? null : dto.Content.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            IsHidden = false,
            OrderId = orderId
        };

        reviewRepo.Add(review);
        await reviewRepo.AddAuditAsync(new ReviewAudit
        {
            Review = review,
            Action = ReviewAction.Created,
            ActorUserId = user.Id,
            ActorEmail = user.Email ?? string.Empty,
            CreatedAt = now,
            NewRating = dto.Rating,
            NewTitle = dto.Title,
            NewContent = dto.Content,
            OldHidden = false,
            NewHidden = false,
            OldResponse = null,
            NewResponse = review.AdminResponse
        });

        if (!await reviewRepo.SaveChangesAsync())
        {
            return BadRequest("Problem creating review");
        }

        await reviewRepo.UpdateProductRatingAsync(productId);
        await reviewRepo.SaveChangesAsync();

        var saved = await reviewRepo.GetReviewByIdAsync(review.Id);
        return Ok(saved?.ToDto(user.Id) ?? review.ToDto(user.Id));
    }

    [InvalidateCache("api/products|")]
    [HttpPut("/api/products/{productId:int}/reviews/{id:int}")]
    public async Task<ActionResult<ReviewDto>> UpdateReview(int productId, int id, [FromBody] CreateReviewDto dto)
    {
        var user = await userManager.GetUserByEmail(User);
        var review = await reviewRepo.GetReviewByIdAsync(id);
        if (review == null || review.ProductId != productId) return NotFound();

        var isOwner = review.UserId == user.Id;
        var isAdmin = User.IsInRole("Admin");
        if (!isOwner && !isAdmin) return Forbid();

        var now = DateTime.UtcNow;
        var audit = new ReviewAudit
        {
            ReviewId = review.Id,
            Action = ReviewAction.Updated,
            ActorUserId = user.Id,
            ActorEmail = user.Email ?? string.Empty,
            CreatedAt = now,
            OldRating = review.Rating,
            OldTitle = review.Title,
            OldContent = review.Content,
            NewRating = dto.Rating,
            NewTitle = dto.Title,
            NewContent = dto.Content,
            OldHidden = review.IsHidden,
            NewHidden = review.IsHidden,
            OldResponse = review.AdminResponse,
            NewResponse = review.AdminResponse
        };

        review.Rating = dto.Rating;
        review.Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title.Trim();
        review.Content = string.IsNullOrWhiteSpace(dto.Content) ? null : dto.Content.Trim();
        review.UpdatedAt = now;

        reviewRepo.Update(review);
        await reviewRepo.AddAuditAsync(audit);

        if (!await reviewRepo.SaveChangesAsync())
        {
            return BadRequest("Problem updating review");
        }

        await reviewRepo.UpdateProductRatingAsync(productId);
        await reviewRepo.SaveChangesAsync();

        var saved = await reviewRepo.GetReviewByIdAsync(review.Id);
        return Ok(saved?.ToDto(user.Id) ?? review.ToDto(user.Id));
    }

    [InvalidateCache("api/products|")]
    [HttpDelete("/api/products/{productId:int}/reviews/{id:int}")]
    public async Task<ActionResult> DeleteReview(int productId, int id)
    {
        var user = await userManager.GetUserByEmail(User);
        var review = await reviewRepo.GetReviewByIdAsync(id);
        if (review == null || review.ProductId != productId) return NotFound();

        var isOwner = review.UserId == user.Id;
        var isAdmin = User.IsInRole("Admin");
        if (!isOwner && !isAdmin) return Forbid();

        var audit = new ReviewAudit
        {
            ReviewId = review.Id,
            Action = ReviewAction.Deleted,
            ActorUserId = user.Id,
            ActorEmail = user.Email ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            OldRating = review.Rating,
            OldTitle = review.Title,
            OldContent = review.Content,
            OldHidden = review.IsHidden,
            OldResponse = review.AdminResponse,
            NewResponse = null,
            NewHidden = true
        };

        reviewRepo.Remove(review);
        await reviewRepo.AddAuditAsync(audit);

        if (!await reviewRepo.SaveChangesAsync())
        {
            return BadRequest("Problem deleting review");
        }

        await reviewRepo.UpdateProductRatingAsync(productId);
        await reviewRepo.SaveChangesAsync();

        return NoContent();
    }

    private async Task<string?> GetCurrentUserIdAsync()
    {
        if (!User.Identity?.IsAuthenticated ?? true) return null;
        var user = await userManager.GetUserByEmail(User);
        return user.Id;
    }
}
