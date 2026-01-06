using Api.Dtos;
using Domain.Orders;
using Domain.Products;

namespace Tests.Data.Orders;

public static class OrderData
{
    public static Order CreateTestOrder(Guid userId, List<OrderItem> items) =>
        Order.New(
            new OrderId(Guid.NewGuid()),
            userId,
            "123 Test Street, Test City, TC 12345",
            "Test order notes",
            items
        );

    public static OrderItem CreateTestOrderItem(OrderId orderId, ProductId productId) =>
        OrderItem.New(orderId, productId, 2, 999.99m);

    public static CreateOrderDto CreateTestOrderDto() =>
        new(
            "456 Test Avenue, Test Town, TT 67890",
            "Please deliver during business hours"
        );
    
    public static OrderItem CreateTestOrderItem(Guid orderId, Guid productId) =>
        OrderItem.New(new OrderId(orderId), new ProductId(productId), 2, 999.99m);
    
    public static OrderItem CreateTestOrderItem(Guid productId) =>
        CreateTestOrderItem(Guid.NewGuid(), productId);

    public static UpdateOrderStatusDto CreateUpdateOrderStatusDto(string status) =>
        new(status);
    
    public static Order CreateOrderWithOneItem(Guid userId, Guid productId, int quantity = 1)
    {
        var orderId = OrderId.New();
        var items = new List<OrderItem>
        {
            CreateTestOrderItem(orderId, new ProductId(productId))
        };
        return CreateTestOrder(userId, items); 
    }

}