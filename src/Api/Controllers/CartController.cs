using Api.Dtos;
using Api.Modules.Errors;
using Application.Carts.Commands;
using Application.Carts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("cart")]
[Authorize(Roles = "User,Manager,Admin")]
public class CartController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartDto>> GetMyCart(CancellationToken cancellationToken)
    {
        var query = new GetMyCartQuery();
        var cart = await sender.Send(query, cancellationToken);

        return cart.Match<ActionResult<CartDto>>(
            c => CartDto.FromDomainModel(c),
            () => NotFound("Cart not found"));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddToCart(
        [FromBody] AddToCartDto request,
        CancellationToken cancellationToken)
    {
        var command = new AddToCartCommand(request.ProductId, request.Quantity);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<CartDto>>(
            c => CartDto.FromDomainModel(c),
            e => e.ToObjectResult());
    }

    [HttpPut("items/{cartItemId:guid}")]
    public async Task<ActionResult<CartDto>> UpdateCartItem(
        [FromRoute] Guid cartItemId,
        [FromBody] UpdateCartItemDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCartItemCommand(cartItemId, request.Quantity);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<CartDto>>(
            c => CartDto.FromDomainModel(c),
            e => e.ToObjectResult());
    }

    [HttpDelete("items/{cartItemId:guid}")]
    public async Task<ActionResult<CartDto>> RemoveFromCart(
        [FromRoute] Guid cartItemId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveFromCartCommand(cartItemId);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<CartDto>>(
            c => CartDto.FromDomainModel(c),
            e => e.ToObjectResult());
    }

    [HttpDelete]
    public async Task<ActionResult<CartDto>> ClearCart(CancellationToken cancellationToken)
    {
        var command = new ClearCartCommand();
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<CartDto>>(
            c => CartDto.FromDomainModel(c),
            e => e.ToObjectResult());
    }
}