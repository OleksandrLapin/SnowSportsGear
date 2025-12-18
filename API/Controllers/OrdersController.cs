using API.DTOs;
using API.Extensions;
using API.Specifications;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class OrdersController(
    ICartService cartService,
    IUnitOfWork unit,
    IProductRepository productRepository,
    IReviewRepository reviewRepository) : BaseApiController
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
                PictureUrl = item.PictureUrl
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
            Status = OrderStatus.PaymentReceived
        };

        // decrement stock
        foreach (var item in items)
        {
            var productItem = await productRepository.GetProductWithVariantsAsync(item.ItemOrdered.ProductId);
            if (productItem != null)
            {
                var variant = productItem.Variants.FirstOrDefault(v => v.Size == item.Size);
                if (variant != null)
                {
                    variant.QuantityInStock -= item.Quantity;
                    unit.Repository<Product>().Update(productItem);
                }
            }
        }

        unit.Repository<Order>().Add(order);

        if (await unit.Complete())
        {
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
}
