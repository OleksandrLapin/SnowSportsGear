using Core.Entities;

namespace Core.Specifications;

public class AdminReviewSpecification : BaseSpecification<ProductReview>
{
    public AdminReviewSpecification(AdminReviewSpecParams spec)
        : base(r =>
            (spec.ProductId == null || r.ProductId == spec.ProductId) &&
            (string.IsNullOrEmpty(spec.Brand) || r.Product.Brand == spec.Brand) &&
            (string.IsNullOrEmpty(spec.Type) || r.Product.Type == spec.Type) &&
            (!spec.MinRating.HasValue || r.Rating >= spec.MinRating.Value) &&
            (!spec.MaxRating.HasValue || r.Rating <= spec.MaxRating.Value) &&
            (string.IsNullOrEmpty(spec.Status) ||
                spec.Status.Equals("all", StringComparison.OrdinalIgnoreCase) ||
                (spec.Status.Equals("hidden", StringComparison.OrdinalIgnoreCase) && r.IsHidden) ||
                (spec.Status.Equals("published", StringComparison.OrdinalIgnoreCase) && !r.IsHidden)) &&
            (string.IsNullOrEmpty(spec.Search) ||
                (r.Title != null && r.Title.Contains(spec.Search)) ||
                (r.Content != null && r.Content.Contains(spec.Search))) &&
            (string.IsNullOrEmpty(spec.UserEmail) || r.User.Email == spec.UserEmail) &&
            (!spec.From.HasValue || r.CreatedAt >= spec.From.Value) &&
            (!spec.To.HasValue || r.CreatedAt <= spec.To.Value)
        )
    {
        AddInclude(r => r.User);
        AddInclude(r => r.Product);

        AddOrder(spec.Sort);
        ApplyPaging(spec.PageSize * (spec.PageIndex - 1), spec.PageSize);
    }

    private void AddOrder(string sort)
    {
        switch (sort?.ToLowerInvariant())
        {
            case "ratingdesc":
                AddOrderByDescending(r => r.Rating);
                break;
            case "ratingasc":
                AddOrderBy(r => r.Rating);
                break;
            case "oldest":
                AddOrderBy(r => r.CreatedAt);
                break;
            default:
                AddOrderByDescending(r => r.CreatedAt);
                break;
        }
    }
}
