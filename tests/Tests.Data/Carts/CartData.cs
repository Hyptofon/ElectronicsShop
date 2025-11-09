using Api.Dtos;
using Domain.Cart;
using Domain.Products;

namespace Tests.Data.Carts;

public static class CartData
{
    public static Cart CreateTestCart(Guid userId) => Cart.New(userId);

    public static CartItem CreateTestCartItem(CartId cartId, ProductId productId, int quantity = 1) =>
        CartItem.New(cartId, productId, quantity);

    public static AddToCartDto CreateAddToCartDto(Guid productId, int quantity = 1) =>
        new(productId, quantity);

    public static UpdateCartItemDto CreateUpdateCartItemDto(int quantity) =>
        new(quantity);
}