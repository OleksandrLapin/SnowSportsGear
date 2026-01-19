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

            await unit.Complete();

            var tokens = NotificationTokenBuilder.BuildOrderTokens(order, notificationOptions.Value);
            tokens["OrderStatus"] = "Refunded";

            await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
                NotificationTemplateKeys.OrderStatusUpdated,
                order.BuyerEmail,
                tokens));

            var adminRequests = await BuildAdminRequestsAsync(userManager, NotificationTemplateKeys.AdminRefundSuccess, tokens);
            await notificationService.SendBulkAsync(adminRequests);

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
}
