namespace Core.Settings;

public class NotificationSettings
{
    public string StoreName { get; set; } = "SnowSports Gear";
    public string StoreUrl { get; set; } = "http://localhost:4200";
    public string SupportEmail { get; set; } = "support@snowsportsgear.com";
    public string SupportPhone { get; set; } = "+1 (555) 000-0000";
    public string LogoUrl { get; set; } = string.Empty;
    public bool RequireTwoFactorOnLogin { get; set; } = true;
    public int SecurityCodeExpiryMinutes { get; set; } = 30;
    public int DeletionRequestSlaDays { get; set; } = 7;
    public int LowStockThreshold { get; set; } = 5;
}
