using Domain.Categories;
using Domain.Products;

namespace Infrastructure.Persistence.Extensions;

public static class ProductQueryExtensions
{
    public static IQueryable<Product> WithSearchTerm(this IQueryable<Product> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var lowerTerm = searchTerm.ToLower();
        return query.Where(x => 
            x.Name.ToLower().Contains(lowerTerm) || 
            x.Description.ToLower().Contains(lowerTerm) ||
            (x.Brand != null && x.Brand.ToLower().Contains(lowerTerm)) ||
            (x.Model != null && x.Model.ToLower().Contains(lowerTerm)));
    }

    public static IQueryable<Product> InCategory(this IQueryable<Product> query, Guid? categoryId)
    {
        if (!categoryId.HasValue)
            return query;

        var categoryIdObj = new CategoryId(categoryId.Value);
        return query.Where(p => p.Categories != null && p.Categories.Any(c => c.CategoryId == categoryIdObj));
    }

    public static IQueryable<Product> WithPriceRange(this IQueryable<Product> query, decimal? minPrice, decimal? maxPrice)
    {
        if (minPrice.HasValue)
            query = query.Where(x => x.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(x => x.Price <= maxPrice.Value);

        return query;
    }

    public static IQueryable<Product> WithBrand(this IQueryable<Product> query, string? brand)
    {
        if (string.IsNullOrWhiteSpace(brand))
            return query;

        var lowerBrand = brand.ToLower();
        return query.Where(x => x.Brand != null && x.Brand.ToLower() == lowerBrand);
    }
}