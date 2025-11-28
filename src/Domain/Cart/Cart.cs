namespace Domain.Cart;

public class Cart
{
    public CartId Id { get; }
    public Guid UserId { get; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<CartItem> Items { get; private set; } = [];

    private Cart(CartId id, Guid userId, DateTime createdAt, DateTime? updatedAt)
    {
        Id = id;
        UserId = userId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Cart New(Guid userId)
        => new(CartId.New(), userId, DateTime.UtcNow, null);

    public void AddItem(CartItem item)
    {
        var existingItem = Items.FirstOrDefault(x => x.ProductId == item.ProductId);
        
        if (existingItem != null)
        {
            existingItem.IncreaseQuantity(item.Quantity);
        }
        else
        {
            Items.Add(item);
        }
        
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateItemQuantity(Guid cartItemId, int quantity)
    {
        var item = Items.FirstOrDefault(x => x.Id.Value == cartItemId);
        
        if (item != null)
        {
            item.UpdateQuantity(quantity);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void RemoveItem(Guid cartItemId)
    {
        var item = Items.FirstOrDefault(x => x.Id.Value == cartItemId);
        if (item != null)
        {
            Items.Remove(item);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Clear()
    {
        Items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }
}