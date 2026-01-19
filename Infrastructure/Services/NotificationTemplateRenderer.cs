using System.Text;
using System.Text.RegularExpressions;
using Core.Entities.Notifications;
using Core.Interfaces;
using Core.Models.Notifications;
using Core.Settings;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class NotificationTemplateRenderer : INotificationTemplateRenderer
{
    private readonly NotificationSettings settings;

    public NotificationTemplateRenderer(IOptions<NotificationSettings> settings)
    {
        this.settings = settings.Value;
    }

    public RenderedNotification Render(NotificationTemplate template, IDictionary<string, string> tokens)
    {
        var mergedTokens = BuildTokenMap(tokens);
        var subject = ReplaceTokens(template.Subject, mergedTokens);
        var headline = ReplaceTokens(template.Headline, mergedTokens);
        var body = ReplaceTokens(template.Body, mergedTokens);
        var ctaLabel = ReplaceTokens(template.CtaLabel ?? string.Empty, mergedTokens);
        var ctaUrl = ReplaceTokens(template.CtaUrl ?? string.Empty, mergedTokens);
        var footer = ReplaceTokens(template.Footer ?? string.Empty, mergedTokens);

        var htmlBody = BuildHtml(subject, headline, body, ctaLabel, ctaUrl, footer);
        var textBody = BuildText(headline, body, ctaLabel, ctaUrl, footer);

        return new RenderedNotification(subject, htmlBody, textBody);
    }

    private Dictionary<string, string> BuildTokenMap(IDictionary<string, string> tokens)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["StoreName"] = settings.StoreName,
            ["SupportEmail"] = settings.SupportEmail,
            ["SupportPhone"] = settings.SupportPhone,
            ["AppUrl"] = settings.StoreUrl,
            ["Year"] = DateTime.UtcNow.Year.ToString()
        };

        foreach (var pair in tokens)
        {
            map[pair.Key] = pair.Value;
        }

        return map;
    }

    private static string ReplaceTokens(string input, IDictionary<string, string> tokens)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        return Regex.Replace(input, "{{(.*?)}}", match =>
        {
            var key = match.Groups[1].Value.Trim();
            return tokens.TryGetValue(key, out var value) ? value : string.Empty;
        });
    }

    private string BuildHtml(string subject, string headline, string body, string ctaLabel, string ctaUrl, string footer)
    {
        var ctaBlock = string.IsNullOrWhiteSpace(ctaUrl) || string.IsNullOrWhiteSpace(ctaLabel)
            ? string.Empty
            : $"<div class=\"cta\"><a href=\"{ctaUrl}\" class=\"button\">{ctaLabel}</a></div>";

        var logoBlock = string.IsNullOrWhiteSpace(settings.LogoUrl)
            ? $"<div class=\"logo\">{settings.StoreName}</div>"
            : $"<img src=\"{settings.LogoUrl}\" alt=\"{settings.StoreName}\" class=\"logo-image\" />";

        var footerBlock = string.IsNullOrWhiteSpace(footer)
            ? string.Empty
            : $"<p class=\"footer-text\">{footer}</p>";

        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html>");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        builder.AppendLine($"  <title>{subject}</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    body { margin:0; padding:0; background:#f4f4f7; font-family: \"Segoe UI\", Arial, sans-serif; color:#111827; }");
        builder.AppendLine("    .wrapper { width:100%; background:#f4f4f7; padding:32px 16px; }");
        builder.AppendLine("    .card { max-width:640px; margin:0 auto; background:#ffffff; border-radius:14px; border:1px solid #e5e7eb; padding:32px; }");
        builder.AppendLine("    .logo { font-size:20px; font-weight:700; color:#0f172a; margin-bottom:24px; }");
        builder.AppendLine("    .logo-image { max-height:36px; margin-bottom:24px; }");
        builder.AppendLine("    h1 { font-size:22px; margin:0 0 12px 0; color:#0f172a; }");
        builder.AppendLine("    .content { font-size:15px; line-height:1.6; color:#374151; }");
        builder.AppendLine("    .cta { margin:24px 0; }");
        builder.AppendLine("    .button { display:inline-block; padding:12px 22px; background:#0f766e; color:#ffffff; text-decoration:none; border-radius:999px; font-weight:600; }");
        builder.AppendLine("    .footer { margin-top:24px; font-size:12px; color:#6b7280; }");
        builder.AppendLine("    .footer-text { margin:4px 0; }");
        builder.AppendLine("    .preheader { display:none; max-height:0; overflow:hidden; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine($"  <span class=\"preheader\">{subject}</span>");
        builder.AppendLine("  <div class=\"wrapper\">");
        builder.AppendLine("    <div class=\"card\">");
        builder.AppendLine($"      {logoBlock}");
        builder.AppendLine($"      <h1>{headline}</h1>");
        builder.AppendLine($"      <div class=\"content\">{body}</div>");
        if (!string.IsNullOrWhiteSpace(ctaBlock))
        {
            builder.AppendLine($"      {ctaBlock}");
        }
        builder.AppendLine("      <div class=\"footer\">");
        if (!string.IsNullOrWhiteSpace(footerBlock))
        {
            builder.AppendLine($"        {footerBlock}");
        }
        builder.AppendLine($"        <p class=\"footer-text\">{settings.StoreName} - {settings.StoreUrl}</p>");
        builder.AppendLine($"        <p class=\"footer-text\">Support: {settings.SupportEmail}</p>");
        builder.AppendLine("      </div>");
        builder.AppendLine("    </div>");
        builder.AppendLine("  </div>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    private static string BuildText(string headline, string body, string ctaLabel, string ctaUrl, string footer)
    {
        var builder = new StringBuilder();
        builder.AppendLine(headline);
        builder.AppendLine();
        builder.AppendLine(StripHtml(body));
        builder.AppendLine();

        if (!string.IsNullOrWhiteSpace(ctaLabel) && !string.IsNullOrWhiteSpace(ctaUrl))
        {
            builder.AppendLine($"{ctaLabel}: {ctaUrl}");
            builder.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(footer))
        {
            builder.AppendLine(StripHtml(footer));
        }

        return builder.ToString().Trim();
    }

    private static string StripHtml(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        return Regex.Replace(input, "<.*?>", string.Empty);
    }
}
