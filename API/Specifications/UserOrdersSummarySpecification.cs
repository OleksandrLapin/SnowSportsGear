using API.DTOs;
using Core.Entities.OrderAggregate;
using Core.Specifications;

namespace API.Specifications;

public class UserOrdersSummarySpecification : BaseSpecification<Order, OrderSummaryDto>
{
    public UserOrdersSummarySpecification(string email) : base(o => o.BuyerEmail == email)
    {
        AddInclude(o => o.DeliveryMethod);
        AddOrderByDescending(o => o.OrderDate);
        ApplyNoTracking();
        ApplySplitQuery();
        AddSelect(o => new OrderSummaryDto
        {
            Id = o.Id,
            BuyerEmail = o.BuyerEmail,
            OrderDate = o.OrderDate,
            Status = o.Status.ToString(),
            Total = o.Subtotal - o.Discount + (o.DeliveryMethod != null ? o.DeliveryMethod.Price : 0),
            TotalItems = o.OrderItems.Sum(i => i.Quantity),
            PreviewItems = o.OrderItems
                .Take(3)
                .Select(i => new OrderItemPreviewDto
                {
                    ProductId = i.ItemOrdered.ProductId,
                    ProductName = i.ItemOrdered.ProductName,
                    PictureUrl = i.ItemOrdered.PictureUrl,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    Size = i.Size
                })
                .ToList()
        });
    }
}
