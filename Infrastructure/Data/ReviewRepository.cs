using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ReviewRepository(StoreContext context) : IReviewRepository
{
    public void Add(ProductReview review)
    {
        context.ProductReviews.Add(review);
    }

    public void Update(ProductReview review)
    {
        context.ProductReviews.Update(review);
    }

    public void Remove(ProductReview review)
    {
        context.ProductReviews.Remove(review);
    }

    public async Task<(IReadOnlyList<ProductReview> Data, int Count)> GetProductReviewsAsync(ReviewSpecParams spec)
    {
        var query = context.ProductReviews.AsNoTracking()
            .Include(r => r.User)
            .Where(r => r.ProductId == spec.ProductId && !r.IsHidden);

        query = ApplySort(query, spec.Sort);

        var count = await query.CountAsync();

        var data = await query
            .Skip(spec.PageSize * (spec.PageIndex - 1))
            .Take(spec.PageSize)
            .ToListAsync();

        return (data, count);
    }

    public async Task<(IReadOnlyList<ProductReview> Data, int Count)> GetAdminReviewsAsync(AdminReviewSpecParams spec)
    {
        var query = context.ProductReviews.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Product)
            .AsQueryable();

        if (spec.ProductId.HasValue)
        {
            query = query.Where(r => r.ProductId == spec.ProductId.Value);
        }

        if (!string.IsNullOrEmpty(spec.Brand))
        {
            query = query.Where(r => r.Product.Brand == spec.Brand);
        }

        if (!string.IsNullOrEmpty(spec.Type))
        {
            query = query.Where(r => r.Product.Type == spec.Type);
        }

        if (spec.MinRating.HasValue)
        {
            query = query.Where(r => r.Rating >= spec.MinRating.Value);
        }

        if (spec.MaxRating.HasValue)
        {
            query = query.Where(r => r.Rating <= spec.MaxRating.Value);
        }

        if (!string.IsNullOrEmpty(spec.Status) && !spec.Status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var isHidden = spec.Status.Equals("hidden", StringComparison.OrdinalIgnoreCase);
            query = query.Where(r => r.IsHidden == isHidden);
        }

        if (!string.IsNullOrEmpty(spec.Search))
        {
            query = query.Where(r => (r.Title != null && r.Title.Contains(spec.Search)) ||
                                     (r.Content != null && r.Content.Contains(spec.Search)));
        }

        if (!string.IsNullOrEmpty(spec.UserEmail))
        {
            query = query.Where(r => r.User.Email == spec.UserEmail);
        }

        if (spec.From.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= spec.From.Value);
        }

        if (spec.To.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= spec.To.Value);
        }

        query = ApplySort(query, spec.Sort);

        var count = await query.CountAsync();

        var data = await query
            .Skip(spec.PageSize * (spec.PageIndex - 1))
            .Take(spec.PageSize)
            .ToListAsync();

        return (data, count);
    }

    public async Task<ProductReview?> GetUserReviewForProductAsync(string userId, int productId)
    {
        return await context.ProductReviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId);
    }

    public async Task<ProductReview?> GetReviewByIdAsync(int id)
    {
        return await context.ProductReviews
            .Include(r => r.User)
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> HasUserPurchasedProductAsync(string email, int productId)
    {
        return await context.Orders
            .Include(o => o.OrderItems)
            .AnyAsync(o =>
                o.BuyerEmail == email &&
                (o.Status == OrderStatus.PaymentReceived || o.Status == OrderStatus.Refunded) &&
                o.OrderItems.Any(i => i.ItemOrdered.ProductId == productId));
    }

    public async Task UpdateProductRatingAsync(int productId)
    {
        var stats = await context.ProductReviews
            .Where(r => r.ProductId == productId && !r.IsHidden)
            .GroupBy(r => r.ProductId)
            .Select(g => new
            {
                Average = g.Average(r => r.Rating),
                Count = g.Count()
            })
            .FirstOrDefaultAsync();

        var product = await context.Products.FindAsync(productId);
        if (product == null) return;

        product.RatingAverage = stats?.Average ?? 0;
        product.RatingCount = stats?.Count ?? 0;
    }

    public async Task AddAuditAsync(ReviewAudit audit)
    {
        await context.ReviewAudits.AddAsync(audit);
    }

    public async Task<IReadOnlyList<ReviewAudit>> GetAuditTrailAsync(int reviewId)
    {
        return await context.ReviewAudits
            .AsNoTracking()
            .Where(a => a.ReviewId == reviewId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ProductReview>> GetUserReviewsAsync(string userId, IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        return await context.ProductReviews
            .AsNoTracking()
            .Where(r => r.UserId == userId && ids.Contains(r.ProductId))
            .ToListAsync();
    }

    public async Task<int?> GetOrderIdForUserProductAsync(string email, int productId, int? orderId = null)
    {
        var query = context.Orders
            .Include(o => o.OrderItems)
            .Where(o =>
                o.BuyerEmail == email &&
                (o.Status == OrderStatus.PaymentReceived || o.Status == OrderStatus.Refunded) &&
                o.OrderItems.Any(i => i.ItemOrdered.ProductId == productId));

        if (orderId.HasValue)
        {
            var match = await query.FirstOrDefaultAsync(o => o.Id == orderId.Value);
            return match?.Id;
        }

        var latest = await query
            .OrderByDescending(o => o.OrderDate)
            .FirstOrDefaultAsync();

        return latest?.Id;
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }

    private static IQueryable<ProductReview> ApplySort(IQueryable<ProductReview> query, string? sort)
    {
        return sort?.ToLowerInvariant() switch
        {
            "ratingdesc" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            "ratingasc" => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            "oldest" => query.OrderBy(r => r.CreatedAt),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };
    }
}
