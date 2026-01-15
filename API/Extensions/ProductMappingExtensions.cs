using API.DTOs;
using Core.Entities;

namespace API.Extensions;

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product, string? baseUrl = null)
    {
        var hasStoredImage = !string.IsNullOrEmpty(product.PictureContentType) ||
            (product.PictureData != null && product.PictureData.Length > 0);
        var variants = EnsureDefaultVariants(product.Variants);
        var totalQuantity = variants.Sum(v => v.QuantityInStock);
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
            SalePrice = product.SalePrice,
            LowestPrice = product.LowestPrice,
            Color = product.Color,
            IsActive = product.IsActive,
            PictureUrl = imagePath,
            Type = product.Type,
            Brand = product.Brand,
            QuantityInStock = totalQuantity,
            Variants = variants,
            RatingAverage = Math.Round(product.RatingAverage, 1),
            RatingCount = product.RatingCount
        };
    }

    private static List<ProductVariantDto> EnsureDefaultVariants(ICollection<ProductVariant>? variants)
    {
        var defaultSizes = new[] { "S", "M", "L", "XL" };
        var dict = (variants ?? Array.Empty<ProductVariant>())
            .GroupBy(v => v.Size.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First().QuantityInStock);

        foreach (var size in defaultSizes)
        {
            var key = size.ToUpperInvariant();
            if (!dict.ContainsKey(key))
            {
                // добавляем отсутствующие размеры с нулевым остатком, чтобы фронт показал "нет в наличии"
                dict[key] = 0;
            }
        }

        return dict.Select(kvp => new ProductVariantDto
        {
            Size = kvp.Key,
            QuantityInStock = kvp.Value
        })
        .OrderBy(v => Array.IndexOf(defaultSizes, v.Size.ToUpperInvariant()))
        .ThenBy(v => v.Size)
        .ToList();
    }
}
