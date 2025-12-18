namespace API.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public string PictureUrl { get; set; } = string.Empty;
    public required string Type { get; set; }
    public required string Brand { get; set; }
    public int QuantityInStock { get; set; }
    public List<ProductVariantDto> Variants { get; set; } = [];
    public double RatingAverage { get; set; }
    public int RatingCount { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? LowestPrice { get; set; }
    public string? Color { get; set; }
}
