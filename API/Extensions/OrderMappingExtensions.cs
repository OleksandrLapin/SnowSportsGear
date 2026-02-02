using System.Collections.Generic;
using API.DTOs;
using API.Helpers;
using Core.Entities;
using Core.Entities.OrderAggregate;

namespace API.Extensions;

public static class OrderMappingExtensions
{
    public static OrderDto ToDto(this Order order, IDictionary<int, ProductReview>? userReviews = null)
    {
        var allowReview = userReviews != null &&
            order.Status is OrderStatus.PaymentReceived
                or OrderStatus.Processing
                or OrderStatus.Packed
                or OrderStatus.Shipped
                or OrderStatus.Delivered
                or OrderStatus.Refunded;

        return new OrderDto
        {
            Id = order.Id,
            BuyerEmail = order.BuyerEmail,
            OrderDate = order.OrderDate,
            ShippingAddress = order.ShippingAddress,
            PaymentSummary = order.PaymentSummary,
            DeliveryMethod = order.DeliveryMethod.Description,
            ShippingPrice = order.DeliveryMethod.Price,
            OrderItems = order.OrderItems.Select(x => x.ToDto(userReviews, allowReview)).ToList(),
            Subtotal = order.Subtotal,
            Discount = order.Discount,
            Total = order.GetTotal(),
            Status = order.Status.ToString(),
            PaymentIntentId = order.PaymentIntentId,
            StatusUpdatedAt = order.StatusUpdatedAt,
            TrackingNumber = order.TrackingNumber,
            TrackingUrl = order.TrackingUrl,
            CancelledBy = order.CancelledBy,
            CancelledReason = order.CancelledReason,
            DeliveryUpdateDetails = order.DeliveryUpdateDetails
        };
    }

    public static OrderItemDto ToDto(this OrderItem orderItem, IDictionary<int, ProductReview>? userReviews = null, bool allowReview = true)
    {
        ProductReview? review = null;
        if (userReviews != null)
        {
            userReviews.TryGetValue(orderItem.ItemOrdered.ProductId, out review);
        }

        return new OrderItemDto
        {
            ProductId = orderItem.ItemOrdered.ProductId,
            ProductName = orderItem.ItemOrdered.ProductName,
            PictureUrl = ImageUrlHelper.NormalizePictureUrl(orderItem.ItemOrdered.PictureUrl, orderItem.ItemOrdered.ProductId),
            Price = orderItem.Price,
            Quantity = orderItem.Quantity,
            Size = orderItem.Size,
            ReviewId = review?.Id,
            ReviewRating = review?.Rating,
            ReviewDate = review?.UpdatedAt ?? review?.CreatedAt,
            CanReview = review == null && allowReview
        };
    }
}
