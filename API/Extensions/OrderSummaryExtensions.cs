using API.DTOs;
using API.Helpers;

namespace API.Extensions;

public static class OrderSummaryExtensions
{
    public static void NormalizePictureUrls(this OrderSummaryDto summary, string? baseUrl = null)
    {
        if (summary.PreviewItems == null) return;
        var normalizedBase = NormalizeBaseUrl(baseUrl);

        foreach (var item in summary.PreviewItems)
        {
            var normalized = ImageUrlHelper.NormalizePictureUrl(item.PictureUrl, item.ProductId);
            if (!string.IsNullOrWhiteSpace(normalizedBase) && normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = normalizedBase + normalized;
            }
            item.PictureUrl = normalized;
        }
    }

    public static void NormalizePictureUrls(this IEnumerable<OrderSummaryDto> summaries, string? baseUrl = null)
    {
        foreach (var summary in summaries)
        {
            summary.NormalizePictureUrls(baseUrl);
        }
    }

    private static string? NormalizeBaseUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return null;
        return baseUrl.TrimEnd('/');
    }
}
