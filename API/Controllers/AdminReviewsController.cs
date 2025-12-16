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

[Authorize(Roles = "Admin")]
[Route("api/admin/reviews")]
public class AdminReviewsController(IReviewRepository reviewRepo, UserManager<AppUser> userManager) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<Pagination<ReviewAdminDto>>> GetReviews([FromQuery] AdminReviewSpecParams specParams)
    {
        var (data, count) = await reviewRepo.GetAdminReviewsAsync(specParams);
        var items = data.Select(r => r.ToAdminDto()).ToList();
        var pagination = new Pagination<ReviewAdminDto>(specParams.PageIndex, specParams.PageSize, count, items);
        return Ok(pagination);
    }

    [HttpGet("{id:int}/audits")]
    public async Task<ActionResult<IReadOnlyList<ReviewAuditDto>>> GetAudit(int id)
    {
        var audits = await reviewRepo.GetAuditTrailAsync(id);
        return Ok(audits.Select(a => a.ToDto()).ToList());
    }

    [InvalidateCache("api/products|")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReviewAdminDto>> UpdateReview(int id, [FromBody] AdminUpdateReviewDto dto)
    {
        var actor = await userManager.GetUserByEmail(User);
        var review = await reviewRepo.GetReviewByIdAsync(id);
        if (review == null) return NotFound();

        var now = DateTime.UtcNow;
        var audit = new ReviewAudit
        {
            ReviewId = review.Id,
            Action = ReviewAction.Updated,
            ActorUserId = actor.Id,
            ActorEmail = actor.Email ?? string.Empty,
            CreatedAt = now,
            OldRating = review.Rating,
            OldTitle = review.Title,
            OldContent = review.Content,
            OldHidden = review.IsHidden,
            NewRating = dto.Rating,
            NewTitle = dto.Title,
            NewContent = dto.Content,
            NewHidden = dto.IsHidden,
            OldResponse = review.AdminResponse,
            NewResponse = review.AdminResponse
        };

        review.Rating = dto.Rating;
        review.Title = string.IsNullOrWhiteSpace(dto.Title) ? null : dto.Title.Trim();
        review.Content = string.IsNullOrWhiteSpace(dto.Content) ? null : dto.Content.Trim();
        review.IsHidden = dto.IsHidden;
        review.UpdatedAt = now;

        reviewRepo.Update(review);
        await reviewRepo.AddAuditAsync(audit);

        if (!await reviewRepo.SaveChangesAsync())
        {
            return BadRequest("Problem updating review");
        }

        await reviewRepo.UpdateProductRatingAsync(review.ProductId);
        await reviewRepo.SaveChangesAsync();

        var refreshed = await reviewRepo.GetReviewByIdAsync(review.Id);
        return Ok(refreshed?.ToAdminDto(actor.Id) ?? review.ToAdminDto(actor.Id));
    }

    [InvalidateCache("api/products|")]
    [HttpPatch("{id:int}/visibility")]
    public async Task<ActionResult<ReviewAdminDto>> SetVisibility(int id, [FromBody] ReviewVisibilityDto dto)
    {
        var actor = await userManager.GetUserByEmail(User);
        var review = await reviewRepo.GetReviewByIdAsync(id);
        if (review == null) return NotFound();

        if (review.IsHidden == dto.IsHidden) return Ok(review.ToAdminDto(actor.Id));

        var now = DateTime.UtcNow;
        var audit = new ReviewAudit
        {
            ReviewId = review.Id,
            Action = dto.IsHidden ? ReviewAction.Hidden : ReviewAction.Unhidden,
            ActorUserId = actor.Id,
            ActorEmail = actor.Email ?? string.Empty,
            CreatedAt = now,
            OldRating = review.Rating,
            NewRating = review.Rating,
            OldTitle = review.Title,
            NewTitle = review.Title,
            OldContent = review.Content,
            NewContent = review.Content,
            OldHidden = review.IsHidden,
            NewHidden = dto.IsHidden,
            OldResponse = review.AdminResponse,
            NewResponse = review.AdminResponse
        };

        review.IsHidden = dto.IsHidden;
        review.UpdatedAt = now;

        reviewRepo.Update(review);
        await reviewRepo.AddAuditAsync(audit);

        if (!await reviewRepo.SaveChangesAsync())
        {
            return BadRequest("Problem updating review visibility");
        }

        await reviewRepo.UpdateProductRatingAsync(review.ProductId);
        await reviewRepo.SaveChangesAsync();

        return Ok(review.ToAdminDto(actor.Id));
    }

    [InvalidateCache("api/products|")]
    [HttpPost("{id:int}/reply")]
    public async Task<ActionResult<ReviewAdminDto>> ReplyToReview(int id, [FromBody] ReviewReplyDto dto)
    {
        var actor = await userManager.GetUserByEmail(User);
        var review = await reviewRepo.GetReviewByIdAsync(id);
        if (review == null) return NotFound();

        var now = DateTime.UtcNow;
        var audit = new ReviewAudit
        {
            ReviewId = review.Id,
            Action = ReviewAction.Responded,
            ActorUserId = actor.Id,
            ActorEmail = actor.Email ?? string.Empty,
            CreatedAt = now,
            OldRating = review.Rating,
            NewRating = review.Rating,
            OldTitle = review.Title,
            NewTitle = review.Title,
            OldContent = review.Content,
            NewContent = review.Content,
            OldHidden = review.IsHidden,
            NewHidden = review.IsHidden,
            OldResponse = review.AdminResponse,
            NewResponse = dto.Response
        };

        review.AdminResponse = dto.Response;
        review.AdminResponderId = actor.Id;
        review.AdminResponderEmail = actor.Email;
        review.AdminRespondedAt = string.IsNullOrWhiteSpace(dto.Response) ? null : now;
        review.UpdatedAt = now;

        reviewRepo.Update(review);
        await reviewRepo.AddAuditAsync(audit);

        if (!await reviewRepo.SaveChangesAsync())
        {
            return BadRequest("Problem replying to review");
        }

        return Ok(review.ToAdminDto(actor.Id));
    }

    [InvalidateCache("api/products|")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteReview(int id)
    {
        var actor = await userManager.GetUserByEmail(User);
        var review = await reviewRepo.GetReviewByIdAsync(id);
        if (review == null) return NotFound();

        var audit = new ReviewAudit
        {
            ReviewId = review.Id,
            Action = ReviewAction.Deleted,
            ActorUserId = actor.Id,
            ActorEmail = actor.Email ?? string.Empty,
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

        await reviewRepo.UpdateProductRatingAsync(review.ProductId);
        await reviewRepo.SaveChangesAsync();

        return NoContent();
    }
}
