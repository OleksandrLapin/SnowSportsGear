using API.DTOs;
using API.Extensions;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class OrdersController(
    ICartService cartService,
    IUnitOfWork unit,
    IProductRepository productRepository,
    IReviewRepository reviewRepository,
    UserManager<AppUser> userManager) : BaseApiController
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

            var orderItem = new OrderItem
            {
                ItemOrdered = itemOrdered,
                Price = productItem.Price,
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
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrdersForUser(
        [FromServices] ILogger<OrdersController> log)
    {
        var sw = Stopwatch.StartNew();
        log.LogInformation("GetOrdersForUser started");

        var user = await userManager.GetUserByEmail(User);
        log.LogInformation("GetUserByEmail completed in {Elapsed}ms", sw.ElapsedMilliseconds);

        if (user == null) return Unauthorized();
        var spec = new OrderSpecification(User.GetEmail());

        log.LogInformation("Calling ListAsync(orders) at {Elapsed}ms", sw.ElapsedMilliseconds);
        var orders = await unit.Repository<Order>().ListAsync(spec);
        log.LogInformation("ListAsync(orders) completed in {Elapsed}ms", sw.ElapsedMilliseconds);

        var productIds = orders.SelectMany(o => o.OrderItems)
            .Select(i => i.ItemOrdered.ProductId)
            .Distinct()
            .ToList();

        log.LogInformation("Calling GetUserReviewsAsync at {Elapsed}ms", sw.ElapsedMilliseconds);
        var userReviews = await reviewRepository.GetUserReviewsAsync(user.Id, productIds);
        log.LogInformation("GetUserReviewsAsync completed in {Elapsed}ms", sw.ElapsedMilliseconds);
        var reviewLookup = userReviews.ToDictionary(r => r.ProductId, r => r);

        log.LogInformation("Total mapping completed in {Elapsed}ms", sw.ElapsedMilliseconds);
        var ordersToReturn = orders.Select(o => o.ToDto(reviewLookup)).ToList();

        return Ok(ordersToReturn);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        var spec = new OrderSpecification(User.GetEmail(), id);

        var order = await unit.Repository<Order>().GetEntityWithSpec(spec);

        if (order == null) return NotFound();

        var user = await userManager.GetUserByEmail(User);
        if (user == null) return Unauthorized();

        var productIds = order.OrderItems
            .Select(i => i.ItemOrdered.ProductId)
            .Distinct()
            .ToList();

        var userReviews = await reviewRepository.GetUserReviewsAsync(user.Id, productIds);
        var reviewLookup = userReviews.ToDictionary(r => r.ProductId, r => r);

        return order.ToDto(reviewLookup);
    }
}
