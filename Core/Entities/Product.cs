using Core.Interfaces;

namespace Core.Entities;

public class Product : BaseEntity, IDtoConvertible
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public string? PictureUrl { get; set; }
    public required string Type { get; set; }
    public required string Brand { get; set; }
    public byte[]? PictureData { get; set; }
    public string? PictureContentType { get; set; }
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public double RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? LowestPrice { get; set; }
    public string? Color { get; set; }
}
