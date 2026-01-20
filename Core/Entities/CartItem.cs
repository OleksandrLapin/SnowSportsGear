namespace Core.Entities;

public class CartItem
{
    public int ProductId { get; set; }
    public required string ProductName { get; set; }
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? LowestPrice { get; set; }
    public int? MaxQuantity { get; set; }
    public int Quantity { get; set; }
    public required string PictureUrl { get; set; }
    public required string Brand { get; set; }
    public required string Type { get; set; }
    public required string Size { get; set; } = string.Empty;
}
