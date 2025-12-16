namespace Core.Specifications;

public class ReviewSpecParams : PagingParams
{
    public int ProductId { get; set; }
    public string Sort { get; set; } = "newest";
}
