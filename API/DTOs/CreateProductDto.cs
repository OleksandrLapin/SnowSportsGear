using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace API.DTOs;

public class CreateProductDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
    public decimal Price { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Brand { get; set; } = string.Empty;

    public decimal? SalePrice { get; set; }

    public decimal? LowestPrice { get; set; }

    public string? Color { get; set; }

    public bool IsActive { get; set; } = true;

    public IFormFile? Image { get; set; }

    public string? SizeGuide { get; set; }

    [MinLength(1, ErrorMessage = "At least one variant is required")]
    public List<CreateProductVariantDto> Variants { get; set; } = [];
}
