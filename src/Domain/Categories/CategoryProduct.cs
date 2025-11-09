using Domain.Products;

namespace Domain.Categories;

public class CategoryProduct
{
    public CategoryId CategoryId { get; }
    public Category? Category { get; private set; }

    public ProductId ProductId { get; }
    public Product? Product { get; private set; }

    private CategoryProduct(CategoryId categoryId, ProductId productId)
        => (CategoryId, ProductId) = (categoryId, productId);

    public static CategoryProduct New(CategoryId categoryId, ProductId productId)
        => new(categoryId, productId);
}