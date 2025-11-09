using Domain.Cart;

namespace Api.Dtos;

public record CartDto(Guid Id, IReadOnlyList<CartItemDto> Items, DateTime CreatedAt, DateTime? UpdatedAt)
{
    public static CartDto FromDomainModel(Cart cart)
        => new(
            cart.Id.Value,
            cart.Items.Select(CartItemDto.FromDomainModel).ToList(),
            cart.CreatedAt,
            cart.UpdatedAt);
}

public record CartItemDto(Guid Id, Guid ProductId, int Quantity)
{
    public static CartItemDto FromDomainModel(CartItem item)
        => new(item.Id.Value, item.ProductId.Value, item.Quantity);
}

public record AddToCartDto(Guid ProductId, int Quantity);

public record UpdateCartItemDto(int Quantity);