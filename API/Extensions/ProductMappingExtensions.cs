using API.DTOs;
using Core.Entities;
using Core.Helpers;

namespace API.Extensions;

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product, string? baseUrl = null)
    {
        var hasStoredImage = !string.IsNullOrEmpty(product.PictureContentType) ||
            (product.PictureData != null && product.PictureData.Length > 0);
        var variants = EnsureDefaultVariants(product.Variants, product.Type);
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

    private static List<ProductVariantDto> EnsureDefaultVariants(ICollection<ProductVariant>? variants, string? type)
    {
        var defaultSizes = ProductSizeDefaults.GetSizesForType(type);
        var defaultOrder = defaultSizes.ToArray();
        var allowedSizes = new HashSet<string>(defaultSizes, StringComparer.OrdinalIgnoreCase);
        var dict = (variants ?? Array.Empty<ProductVariant>())
            .Where(v => !ProductSizeDefaults.IsDisallowedSize(v.Size))
            .Where(v => allowedSizes.Contains(v.Size.Trim()))
            .GroupBy(v => v.Size.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().QuantityInStock, StringComparer.OrdinalIgnoreCase);

        foreach (var size in defaultSizes)
        {
            if (!dict.ContainsKey(size))
            {
                dict[size] = 0;
            }
        }

        return dict.Select(kvp => new ProductVariantDto
        {
            Size = kvp.Key,
            QuantityInStock = kvp.Value
        })
        .OrderBy(v => Array.IndexOf(defaultOrder, v.Size))
        .ThenBy(v => v.Size, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }
}
