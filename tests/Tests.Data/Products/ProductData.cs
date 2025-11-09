using Api.Dtos;
using Domain.Categories;
using Domain.Products;

namespace Tests.Data.Products;

public static class ProductData
{
    public static Product FirstTestProduct(List<CategoryId> categoryIds) =>
        Product.New(
            ProductId.New(),
            "Test iPhone 15 Pro",
            "Test latest iPhone model with advanced features",
            999.99m,
            50,
            "Apple",
            "iPhone 15 Pro",
            categoryIds.Select(id => CategoryProduct.New(id, ProductId.New())).ToList()
        );

    public static Product SecondTestProduct(List<CategoryId> categoryIds) =>
        Product.New(
            ProductId.New(),
            "Test Samsung Galaxy S24",
            "Test flagship Samsung smartphone",
            899.99m,
            30,
            "Samsung",
            "Galaxy S24",
            categoryIds.Select(id => CategoryProduct.New(id, ProductId.New())).ToList()
        );

    public static CreateProductDto CreateTestProductDto(List<Guid> categoryIds) =>
        new(
            "Test MacBook Pro 16",
            "Test powerful laptop for professionals",
            2499.99m,
            20,
            "Apple",
            "MacBook Pro 16",
            categoryIds
        );

    public static UpdateProductDto UpdateTestProductDto(List<Guid> categoryIds) =>
        new(
            "Updated Product Name",
            "Updated product description",
            1999.99m,
            25,
            "Updated Brand",
            "Updated Model",
            categoryIds
        );
}