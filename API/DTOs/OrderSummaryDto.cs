namespace API.DTOs;

public class OrderSummaryDto
{
    public int Id { get; set; }
    public string BuyerEmail { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
