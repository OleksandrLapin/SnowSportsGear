using Core.Entities;

namespace API.Helpers;

public static class ImageUrlHelper
{
    public static string NormalizePictureUrl(string? url, int productId)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return BuildProductImagePath(productId);
        }

        var trimmed = url.Trim();

        if (IsInlineImageUrl(trimmed))
        {
            return trimmed;
        }

        trimmed = trimmed.Replace("\\", "/");

        if (HasScheme(trimmed) || trimmed.StartsWith("/", StringComparison.Ordinal))
        {
            return trimmed;
        }

        return "/" + trimmed.TrimStart('/');
    }

    public static string BuildProductPictureUrl(Product product)
    {
        if ((product.PictureData != null && product.PictureData.Length > 0) ||
            !string.IsNullOrWhiteSpace(product.PictureContentType))
        {
            return BuildProductImagePath(product.Id);
        }

        if (!string.IsNullOrWhiteSpace(product.PictureUrl))
        {
            return NormalizePictureUrl(product.PictureUrl, product.Id);
        }

        return "/images/placeholder.png";
    }

    private static string BuildProductImagePath(int productId)
    {
        return $"/api/products/{productId}/image";
    }

    private static bool IsInlineImageUrl(string value)
    {
        return value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("blob:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasScheme(string value)
    {
        return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }
}
