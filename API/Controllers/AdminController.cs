using API.DTOs;
using API.Extensions;
using API.Helpers;
using API.RequestHelpers;
using API.Specifications;
using Core.Constants;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Core.Settings;

namespace API.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(
    IUnitOfWork unit,
    IPaymentService paymentService,
    INotificationService notificationService,
    UserManager<AppUser> userManager,
    IOptions<NotificationSettings> notificationOptions) : BaseApiController
{
    [HttpGet("orders")]
    public async Task<ActionResult<Pagination<OrderSummaryDto>>> GetOrders([FromQuery]OrderSpecParams specParams)
    {
        var summarySpec = new AdminOrdersSummarySpecification(specParams);
        var countSpec = new OrderSpecification(specParams);

        var data = await unit.Repository<Order>().ListAsync(summarySpec);
        var count = await unit.Repository<Order>().CountAsync(countSpec);

        var pagination = new Pagination<OrderSummaryDto>(specParams.PageIndex, specParams.PageSize, count, data);
        return Ok(pagination);
    }

    [HttpGet("orders/{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        var spec = new OrderSpecification(id);
        
        var order = await unit.Repository<Order>().GetEntityWithSpec(spec);

        if (order == null) return BadRequest("No order with that id");

        return order.ToDto();
    }

    [HttpPut("orders/{id:int}/status")]
    public async Task<ActionResult<OrderSummaryDto>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        var spec = new OrderSpecification(id);
        var order = await unit.Repository<Order>().GetEntityWithSpec(spec);

        if (order == null) return BadRequest("No order with that id");
        if (!TryParseStatus(dto.Status, out var newStatus)) return BadRequest("Invalid status");
        if (!CanUpdate(order.Status)) return BadRequest("Order can no longer be updated");

        if (order.Status == newStatus)
        {
            return ToSummary(order);
        }

        order.Status = newStatus;
        order.StatusUpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.TrackingNumber))
        {
            order.TrackingNumber = dto.TrackingNumber.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.TrackingUrl))
        {
            order.TrackingUrl = dto.TrackingUrl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.DeliveryDetails))
        {
            order.DeliveryUpdateDetails = dto.DeliveryDetails.Trim();
        }

        if (newStatus == OrderStatus.Cancelled)
        {
            order.CancelledBy = "Store";
            order.CancelledReason = string.IsNullOrWhiteSpace(dto.CancelReason)
                ? "Order cancelled by the store"
                : dto.CancelReason.Trim();
        }

        if (!await unit.Complete())
        {
            return BadRequest("Problem updating order status");
        }

        var tokens = NotificationTokenBuilder.BuildOrderTokens(order, notificationOptions.Value);

        if (newStatus == OrderStatus.Cancelled)
        {
            tokens["CancelledBy"] = order.CancelledBy ?? "Store";
            tokens["CancelReason"] = order.CancelledReason ?? "Order cancelled";

            await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
                NotificationTemplateKeys.OrderCancelled,
                order.BuyerEmail,
                tokens));

            var adminRequests = await BuildAdminRequestsAsync(userManager, NotificationTemplateKeys.AdminOrderCancelled, tokens);
            await notificationService.SendBulkAsync(adminRequests);

            return ToSummary(order);
        }

        if (newStatus is OrderStatus.Processing or OrderStatus.Packed or OrderStatus.Shipped or OrderStatus.Delivered)
        {
            tokens["OrderStatus"] = FormatStatus(newStatus);
            await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
                NotificationTemplateKeys.OrderStatusUpdated,
                order.BuyerEmail,
                tokens));
        }

        if (newStatus == OrderStatus.Shipped)
        {
            tokens["TrackingNumber"] = order.TrackingNumber ?? "Pending";
            tokens["TrackingUrl"] = order.TrackingUrl ?? notificationOptions.Value.StoreUrl;

            await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
                NotificationTemplateKeys.DeliveryHandedOff,
                order.BuyerEmail,
                tokens));
        }

        if (!string.IsNullOrWhiteSpace(dto.DeliveryDetails))
        {
            tokens["DeliveryDetails"] = order.DeliveryUpdateDetails ?? dto.DeliveryDetails.Trim();
            await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
                NotificationTemplateKeys.DeliveryUpdated,
                order.BuyerEmail,
                tokens));
        }

        return ToSummary(order);
    }

    [HttpPost("orders/refund/{id:int}")]
    public async Task<ActionResult<OrderSummaryDto>> RefundOrder(int id)
    {
        var spec = new OrderSpecification(id);
        
        var order = await unit.Repository<Order>().GetEntityWithSpec(spec);

        if (order == null) return BadRequest("No order with that id");

        var result = await paymentService.RefundPayment(order.PaymentIntentId);

        if (result == "succeeded")
        {
            order.Status = OrderStatus.Refunded;
            order.StatusUpdatedAt = DateTime.UtcNow;

            await unit.Complete();

            var tokens = NotificationTokenBuilder.BuildOrderTokens(order, notificationOptions.Value);
            tokens["OrderStatus"] = "Refunded";

            await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
                NotificationTemplateKeys.OrderStatusUpdated,
                order.BuyerEmail,
                tokens));

            var adminRequests = await BuildAdminRequestsAsync(userManager, NotificationTemplateKeys.AdminRefundSuccess, tokens);
            await notificationService.SendBulkAsync(adminRequests);

            var returnTokens = new Dictionary<string, string>(tokens)
            {
                ["ReturnReason"] = "Refund processed",
                ["ReturnUrl"] = $"{notificationOptions.Value.StoreUrl}/orders/{order.Id}"
            };

            await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
                NotificationTemplateKeys.ReturnRequestCreated,
                order.BuyerEmail,
                returnTokens));

            return ToSummary(order);
        }

        var failTokens = NotificationTokenBuilder.BuildOrderTokens(order, notificationOptions.Value);
        failTokens["ErrorMessage"] = "Refund failed";
        var adminFailRequests = await BuildAdminRequestsAsync(userManager, NotificationTemplateKeys.AdminRefundFailed, failTokens);
        await notificationService.SendBulkAsync(adminFailRequests);

        return BadRequest("Problem refunding order");
    }

    private static OrderSummaryDto ToSummary(Order order)
    {
        var deliveryPrice = order.DeliveryMethod?.Price ?? 0;

        return new OrderSummaryDto
        {
            Id = order.Id,
            BuyerEmail = order.BuyerEmail,
            OrderDate = order.OrderDate,
            Status = order.Status.ToString(),
            Total = order.Subtotal - order.Discount + deliveryPrice
        };
    }

    private static async Task<List<Core.Models.Notifications.NotificationRequest>> BuildAdminRequestsAsync(
        UserManager<AppUser> userManager,
        string templateKey,
        IDictionary<string, string> tokens)
    {
        var admins = await userManager.GetUsersInRoleAsync("Admin");
        return admins
            .Where(a => !string.IsNullOrWhiteSpace(a.Email))
            .Select(a => new Core.Models.Notifications.NotificationRequest(
                templateKey,
                a.Email ?? string.Empty,
                tokens,
                a.Id))
            .ToList();
    }

    private static bool TryParseStatus(string value, out OrderStatus status)
    {
        return Enum.TryParse(value, true, out status);
    }

    private static bool CanUpdate(OrderStatus status)
    {
        return status is not (OrderStatus.Cancelled or OrderStatus.Delivered or OrderStatus.Refunded);
    }

    private static string FormatStatus(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.PaymentReceived => "Payment received",
            OrderStatus.PaymentFailed => "Payment failed",
            OrderStatus.PaymentMismatch => "Payment mismatch",
            OrderStatus.Processing => "In processing",
            OrderStatus.Packed => "Packed",
            OrderStatus.Shipped => "Shipped",
            OrderStatus.Delivered => "Delivered",
            OrderStatus.Cancelled => "Cancelled",
            _ => status.ToString()
        };
    }
}
