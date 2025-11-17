using Domain.Categories;

namespace Domain.Products;

public class Product
{
    public ProductId Id { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public string? Brand { get; private set; }
    public string? Model { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<ProductImage>? Images { get; private set; } = [];
    public ICollection<CategoryProduct>? Categories { get; private set; } = [];
    public ICollection<ProductReview>? Reviews { get; private set; } = [];

    private Product(ProductId id, string name, string description, decimal price, 
        int stockQuantity, string? brand, string? model, DateTime createdAt, DateTime? updatedAt)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        Brand = brand;
        Model = model;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Product New(
        ProductId id,
        string name,
        string description,
        decimal price,
        int stockQuantity,
        string? brand,
        string? model,
        ICollection<CategoryProduct> categories)
        => new(id, name, description, price, stockQuantity, brand, model, DateTime.UtcNow, null)
        {
            Categories = categories
        };

    public void UpdateDetails(string name, string description, decimal price, 
        int stockQuantity, string? brand, string? model)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));
    
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Product description cannot be empty", nameof(description));
    
        if (price <= 0)
            throw new ArgumentException("Price must be greater than zero", nameof(price));
    
        if (stockQuantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));
        
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        Brand = brand;
        Model = model;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStock(int quantity)
    {
        StockQuantity = quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecreaseStock(int quantity)
    {
        if (StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock quantity");
        
        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncreaseStock(int quantity)
    {
        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}