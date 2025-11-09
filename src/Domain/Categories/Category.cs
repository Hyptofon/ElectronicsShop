namespace Domain.Categories;

public class Category
{
    public CategoryId Id { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ICollection<CategoryProduct>? Products { get; private set; } = [];

    private Category(CategoryId id, string name, string? description, DateTime createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
    }

    public static Category New(CategoryId id, string name, string? description)
        => new(id, name, description, DateTime.UtcNow);

    public void UpdateDetails(string name, string? description)
    {
        Name = name;
        Description = description;
    }
}