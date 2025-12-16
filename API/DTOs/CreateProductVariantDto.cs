using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public class CreateProductVariantDto
{
    [Required]
    public string Size { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be >= 0")]
    public int QuantityInStock { get; set; }
}
