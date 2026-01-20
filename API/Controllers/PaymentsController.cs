using API.Extensions;
using API.SignalR;
using API.Helpers;
using Core.Constants;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Core.Settings;
using System.Globalization;
using Stripe;

namespace API.Controllers;

public class PaymentsController(IPaymentService paymentService,
    IUnitOfWork unit, ILogger<PaymentsController> logger,
    IConfiguration config, IHubContext<NotificationHub> hubContext,
    INotificationService notificationService,
    UserManager<AppUser> userManager,
    IOptions<NotificationSettings> notificationOptions) : BaseApiController
{
    private readonly string _whSecret = config["StripeSettings:WhSecret"]!;
    private static readonly CultureInfo CurrencyCulture = CultureInfo.GetCultureInfo("en-US");

    [Authorize]
    [HttpPost("{cartId}")]
    public async Task<ActionResult<ShoppingCart>> CreateOrUpdatePaymentIntent(string cartId)
    {
        var cart = await paymentService.CreateOrUpdatePaymentIntent(cartId);

        if (cart == null) return BadRequest("Problem with your cart");

        return Ok(cart);
    }

    [HttpGet("delivery-methods")]
    public async Task<ActionResult<IReadOnlyList<DeliveryMethod>>> GetDeliveryMethods()
    {
        return Ok(await unit.Repository<DeliveryMethod>().ListAllAsync());
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();

        try
        {
            var stripeEvent = ConstructStripeEvent(json);

            if (stripeEvent.Data.Object is not PaymentIntent intent)
            {
                return BadRequest("Invalid event data");
            }

            switch (stripeEvent.Type)
            {
                case Events.PaymentIntentSucceeded:
                    await HandlePaymentIntentSucceeded(intent);
                    break;
                case Events.PaymentIntentProcessing:
                    await HandlePaymentIntentProcessing(intent);
                    break;
                case Events.PaymentIntentPaymentFailed:
                    await HandlePaymentIntentFailed(intent);
                    break;
                case Events.PaymentIntentCanceled:
                    await HandlePaymentIntentFailed(intent);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe webhook error");
            return StatusCode(StatusCodes.Status500InternalServerError,  "Webhook error");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred");
            return StatusCode(StatusCodes.Status500InternalServerError,  "An unexpected error occurred");
        }
    }

    private async Task HandlePaymentIntentSucceeded(PaymentIntent intent)
    {
        if (intent.Status == "succeeded") 
        {
            var spec = new OrderSpecification(intent.Id, true);

            var order = await unit.Repository<Order>().GetEntityWithSpec(spec)
                ?? throw new Exception("Order not found");

            var orderTotalInCents = (long)Math.Round(order.GetTotal() * 100, 
                MidpointRounding.AwayFromZero);

            if (orderTotalInCents != intent.Amount)
            {
                order.Status = OrderStatus.PaymentMismatch;
            } 
            else
            {
                order.Status = OrderStatus.PaymentReceived;
            }
            order.StatusUpdatedAt = DateTime.UtcNow;

            unit.Repository<Order>().Update(order);

            await unit.Complete();

            await SendPaymentNotificationsAsync(order, intent.Amount, notificationOptions.Value, notificationService, userManager);

            var connectionId = NotificationHub.GetConnectionIdByEmail(order.BuyerEmail);

            if (!string.IsNullOrEmpty(connectionId))
            {
                await hubContext.Clients.Client(connectionId)
                    .SendAsync("OrderCompleteNotification", order.ToDto());
            }
        }
    }

    private Event ConstructStripeEvent(string json)
    {
        try
        {
            return EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], 
                _whSecret);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to construct stripe event");
            throw new StripeException("Invalid signature");
        }
    }

    private async Task HandlePaymentIntentProcessing(PaymentIntent intent)
    {
        if (intent.Status != "processing") return;

        var spec = new OrderSpecification(intent.Id, true);
        var order = await unit.Repository<Order>().GetEntityWithSpec(spec);
        if (order == null) return;

        if (order.Status != OrderStatus.Pending) return;

        var tokens = NotificationTokenBuilder.BuildOrderTokens(order, notificationOptions.Value);

        await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            NotificationTemplateKeys.OrderPaymentPending,
            order.BuyerEmail,
            tokens));
    }

    private async Task HandlePaymentIntentFailed(PaymentIntent intent)
    {
        if (intent.Status != "requires_payment_method" && intent.Status != "canceled") return;

        var spec = new OrderSpecification(intent.Id, true);
        var order = await unit.Repository<Order>().GetEntityWithSpec(spec);
        if (order == null) return;

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Refunded) return;

        order.Status = OrderStatus.PaymentFailed;
        order.StatusUpdatedAt = DateTime.UtcNow;
        unit.Repository<Order>().Update(order);
        await unit.Complete();

        var tokens = NotificationTokenBuilder.BuildOrderTokens(order, notificationOptions.Value);
        tokens["ErrorMessage"] = intent.LastPaymentError?.Message ?? "Payment failed";

        var templateKey = intent.Status == "canceled"
            ? NotificationTemplateKeys.OrderPaymentDeclined
            : NotificationTemplateKeys.OrderPaymentRetry;

        await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            templateKey,
            order.BuyerEmail,
            tokens));

        var adminRequests = await BuildAdminRequestsAsync(userManager, NotificationTemplateKeys.AdminPaymentFailed, tokens);
        await notificationService.SendBulkAsync(adminRequests);
    }

    private static async Task SendPaymentNotificationsAsync(
        Order order,
        long actualAmount,
        NotificationSettings settings,
        INotificationService notificationService,
        UserManager<AppUser> userManager)
    {
        var tokens = NotificationTokenBuilder.BuildOrderTokens(order, settings);

        if (order.Status == OrderStatus.PaymentMismatch)
        {
            tokens["ExpectedAmount"] = order.GetTotal().ToString("C", CurrencyCulture);
            tokens["ActualAmount"] = (actualAmount / 100m).ToString("C", CurrencyCulture);

            await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
                NotificationTemplateKeys.OrderPaymentDeclined,
                order.BuyerEmail,
                tokens));

            var adminRequests = await BuildAdminRequestsAsync(userManager, NotificationTemplateKeys.AdminFeeMismatch, tokens);
            await notificationService.SendBulkAsync(adminRequests);

            return;
        }

        await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            NotificationTemplateKeys.OrderPaymentReceived,
            order.BuyerEmail,
            tokens));

        var adminPaidRequests = await BuildAdminRequestsAsync(userManager, NotificationTemplateKeys.AdminOrderPaid, tokens);
        await notificationService.SendBulkAsync(adminPaidRequests);
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
