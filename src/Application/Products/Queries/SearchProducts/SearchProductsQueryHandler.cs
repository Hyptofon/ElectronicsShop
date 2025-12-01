// Шлях: src/Application/Products/Queries/SearchProducts/SearchProductsQueryHandler.cs
using Application.Common.Interfaces.Queries;
using Domain.Products;
using MediatR;

namespace Application.Products.Queries.SearchProducts;

public class SearchProductsQueryHandler(IProductQueries productQueries) 
    : IRequestHandler<SearchProductsQuery, IReadOnlyList<Product>>
{
    public async Task<IReadOnlyList<Product>> Handle(
        SearchProductsQuery request, 
        CancellationToken cancellationToken)
    {
        return await productQueries.SearchAsync(
            request.SearchTerm,
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            request.Brand,
            cancellationToken);
    }
}