namespace Domain.Products;

public class ProductImage
{
    public ProductImageId Id { get; }
    public string OriginalName { get; }
    public bool IsPrimary { get; private set; }

    public ProductId ProductId { get; }
    public Product? Product { get; private set; }

    private ProductImage(ProductImageId id, string originalName, ProductId productId, bool isPrimary)
    {
        Id = id;
        OriginalName = originalName;
        ProductId = productId;
        IsPrimary = isPrimary;
    }

    public static ProductImage New(ProductId productId, string originalName, bool isPrimary = false)
    {
        return new ProductImage(ProductImageId.New(), originalName, productId, isPrimary);
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void RemoveAsPrimary()
    {
        IsPrimary = false;
    }

    public string GetFilePath()
        => $"products/{ProductId}/{Id}{Path.GetExtension(OriginalName)}";
}