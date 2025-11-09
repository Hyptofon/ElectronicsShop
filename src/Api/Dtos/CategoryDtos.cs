using Domain.Categories;

namespace Api.Dtos;

public record CategoryDto(Guid Id, string Name, string? Description, DateTime CreatedAt)
{
    public static CategoryDto FromDomainModel(Category category)
        => new(category.Id.Value, category.Name, category.Description, category.CreatedAt);
}

public record CreateCategoryDto(string Name, string? Description);

public record UpdateCategoryDto(string Name, string? Description);

public record CategoryProductDto(CategoryDto? Category)
{
    public static CategoryProductDto FromDomainModel(CategoryProduct product)
        => new(product.Category == null ? null : CategoryDto.FromDomainModel(product.Category));
}