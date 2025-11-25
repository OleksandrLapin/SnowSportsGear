using API.DTOs;
using API.Extensions;
using API.RequestHelpers;
using API.Specifications;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(IUnitOfWork unit, IPaymentService paymentService) : BaseApiController
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

            return ToSummary(order);
        }

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
}
