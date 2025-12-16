using Core.Entities;

namespace Core.Specifications;

public class ReviewSpecification : BaseSpecification<ProductReview>
{
    public ReviewSpecification(ReviewSpecParams spec) 
        : base(r => r.ProductId == spec.ProductId && !r.IsHidden)
    {
        AddInclude(r => r.User);
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
