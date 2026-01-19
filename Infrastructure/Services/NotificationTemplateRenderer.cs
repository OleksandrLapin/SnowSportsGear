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
        var brand = "#7d00fa";
        var background = "#f6f0ff";
        var cardBorder = "#e9d7ff";
        var headingColor = "#1f2937";
        var contentColor = "#374151";
        var muted = "#6b7280";
        var codeStyle = "display:inline-block; margin:12px 0; padding:10px 14px; font-family:'Courier New', monospace; font-size:18px; letter-spacing:2px; color:#4c1d95; background:#f3e8ff; border:1px dashed #c4b5fd; border-radius:10px;";

        var normalizedBody = body
            .Replace("<span class=\"code\">", $"<span style=\"{codeStyle}\">", StringComparison.OrdinalIgnoreCase)
            .Replace("<span class='code'>", $"<span style=\"{codeStyle}\">", StringComparison.OrdinalIgnoreCase);

        var ctaBlock = string.IsNullOrWhiteSpace(ctaUrl) || string.IsNullOrWhiteSpace(ctaLabel)
            ? string.Empty
            : $"<div style=\"margin:24px 0;\"><a href=\"{ctaUrl}\" style=\"display:inline-block; padding:12px 24px; background:{brand}; color:#ffffff; text-decoration:none; border-radius:999px; font-weight:600;\">{ctaLabel}</a></div>";

        var logoBlock = string.IsNullOrWhiteSpace(settings.LogoUrl)
            ? $"<div style=\"font-size:20px; font-weight:700; color:{brand}; margin:0 0 20px 0;\">{settings.StoreName}</div>"
            : $"<img src=\"{settings.LogoUrl}\" alt=\"{settings.StoreName}\" style=\"display:block; max-height:36px; margin:0 0 20px 0;\" />";

        var footerBlock = string.IsNullOrWhiteSpace(footer)
            ? string.Empty
            : $"<div style=\"margin-bottom:8px;\">{footer}</div>";

        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html>");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        builder.AppendLine($"  <title>{subject}</title>");
        builder.AppendLine("</head>");
        builder.AppendLine($"<body style=\"margin:0; padding:0; background:{background}; font-family:'Segoe UI', Arial, sans-serif; color:#111827;\">");
        builder.AppendLine($"  <span style=\"display:none; max-height:0; overflow:hidden;\">{subject}</span>");
        builder.AppendLine($"  <table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:{background}; padding:32px 16px;\">");
        builder.AppendLine("    <tr>");
        builder.AppendLine("      <td align=\"center\">");
        builder.AppendLine($"        <table role=\"presentation\" width=\"600\" cellpadding=\"0\" cellspacing=\"0\" style=\"width:100%; max-width:600px; background:#ffffff; border:1px solid {cardBorder}; border-radius:18px; overflow:hidden;\">");
        builder.AppendLine("          <tr>");
        builder.AppendLine($"            <td style=\"height:4px; background:{brand};\"></td>");
        builder.AppendLine("          </tr>");
        builder.AppendLine("          <tr>");
        builder.AppendLine($"            <td style=\"padding:32px;\">");
        builder.AppendLine($"              {logoBlock}");
        builder.AppendLine($"              <h1 style=\"font-size:22px; margin:0 0 12px 0; color:{headingColor};\">{headline}</h1>");
        builder.AppendLine($"              <div style=\"font-size:15px; line-height:1.6; color:{contentColor};\">{normalizedBody}</div>");
        if (!string.IsNullOrWhiteSpace(ctaBlock))
        {
            builder.AppendLine($"              {ctaBlock}");
        }
        builder.AppendLine($"              <div style=\"margin-top:24px; font-size:12px; color:{muted};\">");
        if (!string.IsNullOrWhiteSpace(footerBlock))
        {
            builder.AppendLine($"                {footerBlock}");
        }
        builder.AppendLine($"                <div>{settings.StoreName} - {settings.StoreUrl}</div>");
        builder.AppendLine($"                <div>Support: {settings.SupportEmail}</div>");
        builder.AppendLine("              </div>");
        builder.AppendLine("            </td>");
        builder.AppendLine("          </tr>");
        builder.AppendLine("        </table>");
        builder.AppendLine("      </td>");
        builder.AppendLine("    </tr>");
        builder.AppendLine("  </table>");
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
        var output = Regex.Replace(input, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
        output = Regex.Replace(output, "</p>", "\n\n", RegexOptions.IgnoreCase);
        output = Regex.Replace(output, "</tr>", "\n", RegexOptions.IgnoreCase);
        output = Regex.Replace(output, "</t[dh]>", " | ", RegexOptions.IgnoreCase);
        output = Regex.Replace(output, "<li[^>]*>", "- ", RegexOptions.IgnoreCase);
        output = Regex.Replace(output, "</li>", "\n", RegexOptions.IgnoreCase);
        output = Regex.Replace(output, "</table>", "\n", RegexOptions.IgnoreCase);
        output = Regex.Replace(output, "<.*?>", string.Empty);
        output = Regex.Replace(output, "\\s+\\|\\s+\\n", "\n");
        output = Regex.Replace(output, "\\s*\\|\\s*$", string.Empty);
        output = Regex.Replace(output, "\\n{3,}", "\n\n");
        return output.Trim();
    }
}
