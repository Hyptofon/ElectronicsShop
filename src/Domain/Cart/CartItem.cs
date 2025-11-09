using Domain.Products;

namespace Domain.Cart;

public class CartItem
{
    public CartItemId Id { get; }
    public CartId CartId { get; }
    public ProductId ProductId { get; }
    public int Quantity { get; private set; }

    public Cart? Cart { get; private set; }

    private CartItem(CartItemId id, CartId cartId, ProductId productId, int quantity)
    {
        Id = id;
        CartId = cartId;
        ProductId = productId;
        Quantity = quantity;
    }

    public static CartItem New(CartId cartId, ProductId productId, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");

        return new(CartItemId.New(), cartId, productId, quantity);
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");

        Quantity = quantity;
    }

    public void IncreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");

        Quantity += amount;
    }

    public void DecreaseQuantity(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");
        if (Quantity - amount < 0)
            throw new InvalidOperationException("Quantity cannot be negative");

        Quantity -= amount;
    }
}