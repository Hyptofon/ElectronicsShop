using Domain.Products;
using MediatR;

namespace Application.Products.Queries.SearchProducts;

public record SearchProductsQuery(
    string? SearchTerm,
    Guid? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Brand
) : IRequest<IReadOnlyList<Product>>;