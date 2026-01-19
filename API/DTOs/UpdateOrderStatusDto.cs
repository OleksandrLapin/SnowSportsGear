namespace API.DTOs;

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
    public string? CancelReason { get; set; }
    public string? TrackingNumber { get; set; }
    public string? TrackingUrl { get; set; }
    public string? DeliveryDetails { get; set; }
}
