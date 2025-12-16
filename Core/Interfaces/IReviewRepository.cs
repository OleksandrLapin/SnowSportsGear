using Core.Entities;
using Core.Specifications;

namespace Core.Interfaces;

public interface IReviewRepository
{
    Task<(IReadOnlyList<ProductReview> Data, int Count)> GetProductReviewsAsync(ReviewSpecParams spec);
    Task<ProductReview?> GetUserReviewForProductAsync(string userId, int productId);
    Task<ProductReview?> GetReviewByIdAsync(int id);
    Task<bool> HasUserPurchasedProductAsync(string email, int productId);
    void Add(ProductReview review);
    void Update(ProductReview review);
    void Remove(ProductReview review);
    Task UpdateProductRatingAsync(int productId);
    Task AddAuditAsync(ReviewAudit audit);
    Task<IReadOnlyList<ReviewAudit>> GetAuditTrailAsync(int reviewId);
    Task<(IReadOnlyList<ProductReview> Data, int Count)> GetAdminReviewsAsync(AdminReviewSpecParams spec);
    Task<IReadOnlyList<ProductReview>> GetUserReviewsAsync(string userId, IEnumerable<int> productIds);
    Task<int?> GetOrderIdForUserProductAsync(string email, int productId, int? orderId = null);
    Task<bool> SaveChangesAsync();
}
