using Domain.Products;

namespace Api.Dtos;

public record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string? Brand,
    string? Model,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<ProductImageDto>? Images,
    IReadOnlyList<CategoryProductDto>? Categories,
    double AverageRating,
    int ReviewCount)
{
    public static ProductDto FromDomainModel(Product product)
    {
        var reviews = product.Reviews?.Where(r => r.IsModerated).ToList() ?? new List<ProductReview>();
        var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

        return new ProductDto(
            product.Id.Value,
            product.Name,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.Brand,
            product.Model,
            product.CreatedAt,
            product.UpdatedAt,
            product.Images?.Select(ProductImageDto.FromDomainModel).ToList() ?? [],
            product.Categories?.Select(CategoryProductDto.FromDomainModel).ToList() ?? [],
            averageRating,
            reviews.Count);
    }
}

public record ProductImageDto(Guid Id, string OriginalName, bool IsPrimary, string Url)
{
    public static ProductImageDto FromDomainModel(ProductImage image)
        => new(image.Id.Value, image.OriginalName, image.IsPrimary, $"/uploads/{image.GetFilePath()}");
}

public record CreateProductDto(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string? Brand,
    string? Model,
    IReadOnlyList<Guid> Categories);

public record UpdateProductDto(
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string? Brand,
    string? Model,
    IReadOnlyList<Guid> Categories);