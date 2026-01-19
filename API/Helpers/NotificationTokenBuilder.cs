using System.Globalization;
using System.Text;
using Core.Entities.OrderAggregate;
using Core.Settings;

namespace API.Helpers;

public static class NotificationTokenBuilder
{
    private static readonly CultureInfo CurrencyCulture = CultureInfo.GetCultureInfo("en-US");

    public static Dictionary<string, string> BuildOrderTokens(Order order, NotificationSettings settings)
    {
        var tokens = new Dictionary<string, string>
        {
            ["OrderNumber"] = order.Id.ToString(),
            ["OrderTotal"] = order.GetTotal().ToString("C", CurrencyCulture),
            ["OrderUrl"] = $"{settings.StoreUrl}/orders/{order.Id}",
            ["PaymentUrl"] = $"{settings.StoreUrl}/orders/{order.Id}",
            ["AdminOrderUrl"] = $"{settings.StoreUrl}/admin",
            ["ShippingAddress"] = FormatAddress(order.ShippingAddress),
            ["PaymentSummary"] = FormatPayment(order.PaymentSummary),
            ["OrderSummary"] = BuildOrderSummaryHtml(order.OrderItems)
        };

        return tokens;
    }

    public static string BuildOrderSummaryHtml(IEnumerable<OrderItem> items)
    {
        var builder = new StringBuilder();
        builder.Append("<table style=\"width:100%; border-collapse:collapse;\">");
        builder.Append("<thead><tr>");
        builder.Append("<th style=\"text-align:left; border-bottom:1px solid #e5e7eb; padding:6px;\">Item</th>");
        builder.Append("<th style=\"text-align:left; border-bottom:1px solid #e5e7eb; padding:6px;\">Size</th>");
        builder.Append("<th style=\"text-align:right; border-bottom:1px solid #e5e7eb; padding:6px;\">Qty</th>");
        builder.Append("<th style=\"text-align:right; border-bottom:1px solid #e5e7eb; padding:6px;\">Price</th>");
        builder.Append("</tr></thead><tbody>");

        foreach (var item in items)
        {
            builder.Append("<tr>");
            builder.Append($"<td style=\"padding:6px;\">{item.ItemOrdered.ProductName}</td>");
            builder.Append($"<td style=\"padding:6px;\">{item.Size}</td>");
            builder.Append($"<td style=\"padding:6px; text-align:right;\">{item.Quantity}</td>");
            builder.Append($"<td style=\"padding:6px; text-align:right;\">{item.Price.ToString("C", CurrencyCulture)}</td>");
            builder.Append("</tr>");
        }

        builder.Append("</tbody></table>");
        return builder.ToString();
    }

    private static string FormatAddress(ShippingAddress address)
    {
        var parts = new[]
        {
            address.Name,
            address.Line1,
            address.Line2 ?? string.Empty,
            $"{address.City}, {address.State} {address.PostalCode}",
            address.Country
        };

        return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string FormatPayment(PaymentSummary summary)
    {
        return $"{summary.Brand} ending in {summary.Last4} (exp {summary.ExpMonth:D2}/{summary.ExpYear})";
    }
}
