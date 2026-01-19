namespace Core.Entities.OrderAggregate;

public enum OrderStatus
{
    Pending = 0,
    PaymentReceived = 1,
    PaymentFailed = 2,
    PaymentMismatch = 3,
    Refunded = 4,
    Processing = 5,
    Packed = 6,
    Shipped = 7,
    Delivered = 8,
    Cancelled = 9
}
