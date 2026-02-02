using API.DTOs;
using API.Extensions;
using API.Helpers;
using API.Specifications;
using Core.Constants;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Core.Settings;

namespace API.Controllers;

[Authorize]
public class OrdersController(
    ICartService cartService,
    IUnitOfWork unit,
    IProductRepository productRepository,
    IReviewRepository reviewRepository,
    INotificationService notificationService,
    UserManager<AppUser> userManager,
    IOptions<NotificationSettings> notificationOptions) : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder(CreateOrderDto orderDto)
    {
        var email = User.GetEmail();

        var cart = await cartService.GetCartAsync(orderDto.CartId);

        if (cart == null) return BadRequest("Cart not found");

        if (cart.PaymentIntentId == null) return BadRequest("No payment intent for this order");

        var items = new List<OrderItem>();

        foreach (var item in cart.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Size)) return BadRequest("Size is required for all cart items");

            var productItem = await productRepository.GetProductWithVariantsAsync(item.ProductId);

            if (productItem == null) return BadRequest("Problem with the order");

            var variant = productItem.Variants.FirstOrDefault(v => v.Size == item.Size);
            if (variant == null || variant.QuantityInStock < item.Quantity)
            {
                return BadRequest($"Not enough stock for {productItem.Name} size {item.Size}");
            }

            var itemOrdered = new ProductItemOrdered
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                PictureUrl = ImageUrlHelper.BuildProductPictureUrl(productItem)
            };

            var priceToUse = productItem.SalePrice ?? productItem.Price;
            var orderItem = new OrderItem
            {
                ItemOrdered = itemOrdered,
                Price = priceToUse,
                Quantity = item.Quantity,
                Size = item.Size
            };
            items.Add(orderItem);
        }

        var deliveryMethod = await unit.Repository<DeliveryMethod>().GetByIdAsync(orderDto.DeliveryMethodId);

        if (deliveryMethod == null) return BadRequest("No delivery method selected");

        var order = new Order
        {
            OrderItems = items,
            DeliveryMethod = deliveryMethod,
            ShippingAddress = orderDto.ShippingAddress,
            Subtotal = items.Sum(x => x.Price * x.Quantity),
            Discount = orderDto.Discount,
            PaymentSummary = orderDto.PaymentSummary,
            PaymentIntentId = cart.PaymentIntentId,
            BuyerEmail = email,
            Status = OrderStatus.PaymentReceived,
            StatusUpdatedAt = DateTime.UtcNow
        };

        var lowStockRequests = new List<Core.Models.Notifications.NotificationRequest>();
        var lowStockThreshold = notificationOptions.Value.LowStockThreshold;

        // decrement stock
        foreach (var item in items)
        {
            var productItem = await productRepository.GetProductWithVariantsAsync(item.ItemOrdered.ProductId);
            if (productItem != null)
            {
                var variant = productItem.Variants.FirstOrDefault(v => v.Size == item.Size);
                if (variant != null)
                {
                    var previousQty = variant.QuantityInStock;
                    variant.QuantityInStock -= item.Quantity;
                    unit.Repository<Product>().Update(productItem);

                    if (previousQty > lowStockThreshold && variant.QuantityInStock <= lowStockThreshold)
                    {
                        var tokens = new Dictionary<string, string>
                        {
                            ["ProductName"] = productItem.Name,
                            ["Variant"] = variant.Size,
                            ["Quantity"] = variant.QuantityInStock.ToString(),
                            ["AdminProductUrl"] = $"{notificationOptions.Value.StoreUrl}/admin"
                        };
                        lowStockRequests.AddRange(await BuildAdminRequestsAsync(
                            userManager,
                            NotificationTemplateKeys.AdminInventoryLow,
                            tokens));
                    }
                }
            }
        }

        unit.Repository<Order>().Add(order);

        if (await unit.Complete())
        {
            var tokens = NotificationTokenBuilder.BuildOrderTokens(order, notificationOptions.Value);
            var orderRequest = new Core.Models.Notifications.NotificationRequest(
                NotificationTemplateKeys.OrderCreated,
                order.BuyerEmail,
                tokens);

            await notificationService.SendAsync(orderRequest);

            var adminRequests = await BuildAdminRequestsAsync(userManager, NotificationTemplateKeys.AdminNewOrder, tokens);
            await notificationService.SendBulkAsync(adminRequests);

            if (lowStockRequests.Count > 0)
            {
                await notificationService.SendBulkAsync(lowStockRequests);
            }

            return order;
        }

        return BadRequest("Problem creating order");
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryDto>>> GetOrdersForUser(
        [FromServices] ILogger<OrdersController> log)
    {
        var sw = Stopwatch.StartNew();
        log.LogInformation("GetOrdersForUser started");

        var email = User.GetEmail();
        var spec = new UserOrdersSummarySpecification(email);

        log.LogInformation("Calling ListAsync(user order summaries) at {Elapsed}ms", sw.ElapsedMilliseconds);
        var ordersToReturn = await unit.Repository<Order>().ListAsync(spec);
        log.LogInformation("ListAsync(user order summaries) completed in {Elapsed}ms", sw.ElapsedMilliseconds);

        ordersToReturn.NormalizePictureUrls(GetBaseUrl());
        return Ok(ordersToReturn);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        var spec = new OrderSpecification(User.GetEmail(), id);

        var order = await unit.Repository<Order>().GetEntityWithSpec(spec);

        if (order == null) return NotFound();

        var userId = User.GetUserId();

        var productIds = order.OrderItems
            .Select(i => i.ItemOrdered.ProductId)
            .Distinct()
            .ToList();

        var userReviews = await reviewRepository.GetUserReviewsAsync(userId, productIds);
        var reviewLookup = userReviews.ToDictionary(r => r.ProductId, r => r);

        return order.ToDto(reviewLookup);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<OrderDto>> CancelOrder(int id, [FromBody] CancelOrderDto dto)
    {
        var spec = new OrderSpecification(User.GetEmail(), id);
        var order = await unit.Repository<Order>().GetEntityWithSpec(spec);

        if (order == null) return NotFound("Order not found");
        if (!CanCancel(order.Status)) return BadRequest("Order can no longer be cancelled");

        order.Status = OrderStatus.Cancelled;
        order.StatusUpdatedAt = DateTime.UtcNow;
        order.CancelledBy = "Customer";
        order.CancelledReason = string.IsNullOrWhiteSpace(dto.Reason)
            ? "Customer requested cancellation"
            : dto.Reason.Trim();

        unit.Repository<Order>().Update(order);

        if (!await unit.Complete())
        {
            return BadRequest("Problem cancelling order");
        }

        var tokens = NotificationTokenBuilder.BuildOrderTokens(order, notificationOptions.Value);
        tokens["CancelledBy"] = order.CancelledBy;
        tokens["CancelReason"] = order.CancelledReason ?? "Customer requested cancellation";

        await notificationService.SendAsync(new Core.Models.Notifications.NotificationRequest(
            NotificationTemplateKeys.OrderCancelled,
            order.BuyerEmail,
            tokens));

        var adminRequests = await BuildAdminRequestsAsync(userManager, NotificationTemplateKeys.AdminOrderCancelled, tokens);
        await notificationService.SendBulkAsync(adminRequests);

        return Ok(order.ToDto());
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

    private static bool CanCancel(OrderStatus status)
    {
        return status is not (OrderStatus.Cancelled or OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Refunded);
    }

    private string GetBaseUrl()
    {
        var request = HttpContext.Request;
        var host = request.Host.HasValue ? request.Host.Value : "localhost";
        var scheme = string.IsNullOrEmpty(request.Scheme) ? "https" : request.Scheme;
        return $"{scheme}://{host}/";
    }
}
