using API.DTOs;
using Core.Entities;

namespace API.Extensions;

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product, string? baseUrl = null)
    {
        var hasStoredImage = !string.IsNullOrEmpty(product.PictureContentType) ||
            (product.PictureData != null && product.PictureData.Length > 0);
        var imagePath = hasStoredImage
            ? "api/products/{id}/image"
            : product.PictureUrl?.TrimStart('/') ?? string.Empty;

        if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(imagePath))
        {
            var cleanedBase = baseUrl.TrimEnd('/');
            var cleanedPath = imagePath.Replace("{id}", product.Id.ToString()).TrimStart('/');
            imagePath = $"{cleanedBase}/{cleanedPath}";
        }
        else if (hasStoredImage)
        {
            imagePath = "/" + imagePath.Replace("{id}", product.Id.ToString()).TrimStart('/');
        }
        else if (!string.IsNullOrEmpty(product.PictureUrl))
        {
            imagePath = "/" + imagePath;
        }

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            PictureUrl = imagePath,
            Type = product.Type,
            Brand = product.Brand,
            QuantityInStock = product.QuantityInStock
        };
    }
}
