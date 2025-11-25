using API.DTOs;
using Core.Entities.OrderAggregate;
using Core.Specifications;

namespace API.Specifications;

public class AdminOrdersSummarySpecification : BaseSpecification<Order, OrderSummaryDto>
{
    public AdminOrdersSummarySpecification(OrderSpecParams specParams) : base(x =>
        string.IsNullOrEmpty(specParams.Status) ||
        specParams.Status.Equals("all", StringComparison.OrdinalIgnoreCase) ||
        x.Status == ParseStatus(specParams.Status))
    {
        AddOrderByDescending(x => x.OrderDate);
        ApplyPaging(specParams.PageSize * (specParams.PageIndex - 1), specParams.PageSize);
        AddSelect(o => new OrderSummaryDto
        {
            Id = o.Id,
            BuyerEmail = o.BuyerEmail,
            OrderDate = o.OrderDate,
            Status = o.Status.ToString(),
            Total = o.Subtotal - o.Discount + (o.DeliveryMethod != null ? o.DeliveryMethod.Price : 0)
        });
    }

    private static OrderStatus? ParseStatus(string status)
    {
        if (Enum.TryParse<OrderStatus>(status, true, out var result)) return result;
        return null;
    }
}
