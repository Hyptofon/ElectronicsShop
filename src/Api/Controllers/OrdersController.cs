using Api.Dtos;
using Api.Modules.Errors;
using Application.Orders.Commands;
using Application.Orders.Queries;
using Domain.Orders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("orders")]
[Authorize]
public class OrdersController(ISender sender) : ControllerBase
{
    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetMyOrders(
        CancellationToken cancellationToken)
    {
        var query = new GetMyOrdersQuery();
        var orders = await sender.Send(query, cancellationToken);
        return orders.Select(OrderDto.FromDomainModel).ToList();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetOrderByIdQuery(id);
        var order = await sender.Send(query, cancellationToken);

        return order.Match<ActionResult<OrderDto>>(
            o => OrderDto.FromDomainModel(o),
            () => NotFound());
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand
        {
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<OrderDto>>(
            o => OrderDto.FromDomainModel(o),
            e => e.ToObjectResult());
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new CancelOrderCommand(id);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<OrderDto>>(
            o => OrderDto.FromDomainModel(o),
            e => e.ToObjectResult());
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var query = new GetAllOrdersQuery();
        var orders = await sender.Send(query, cancellationToken);
        return orders.Select(OrderDto.FromDomainModel).ToList();
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetByStatus(
        [FromRoute] string status,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            return BadRequest("Invalid order status");
        }

        var query = new GetOrdersByStatusQuery(orderStatus);
        var orders = await sender.Send(query, cancellationToken);
        return orders.Select(OrderDto.FromDomainModel).ToList();
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateOrderStatusDto request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var orderStatus))
        {
            return BadRequest("Invalid order status");
        }

        var command = new UpdateOrderStatusCommand(id, orderStatus);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<OrderDto>>(
            o => OrderDto.FromDomainModel(o),
            e => e.ToObjectResult());
    }
}