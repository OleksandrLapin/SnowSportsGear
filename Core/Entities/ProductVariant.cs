namespace Core.Entities;

public class ProductVariant : BaseEntity
{
    public required string Size { get; set; }
    public int QuantityInStock { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
