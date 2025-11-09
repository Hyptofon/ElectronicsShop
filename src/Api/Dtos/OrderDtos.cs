using Domain.Orders;

namespace Api.Dtos;

public record OrderDto(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    string ShippingAddress,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<OrderItemDto> Items)
{
    public static OrderDto FromDomainModel(Order order)
        => new(
            order.Id.Value,
            order.UserId,
            order.Status.ToString(),
            order.TotalAmount,
            order.ShippingAddress,
            order.Notes,
            order.CreatedAt,
            order.UpdatedAt,
            order.Items.Select(OrderItemDto.FromDomainModel).ToList());
}

public record OrderItemDto(Guid Id, Guid ProductId, int Quantity, decimal UnitPrice)
{
    public static OrderItemDto FromDomainModel(OrderItem item)
        => new(item.Id.Value, item.ProductId.Value, item.Quantity, item.UnitPrice);
}

public record CreateOrderDto(string ShippingAddress, string? Notes);

public record UpdateOrderStatusDto(string Status);