using Api.Dtos;
using Domain.Categories;

namespace Tests.Data.Categories;

public static class CategoryData
{
    public static Category FirstTestCategory() => 
        Category.New(
            CategoryId.New(),
            "Test Smartphones",
            "Test category for smartphones and mobile devices"
        );

    public static Category SecondTestCategory() => 
        Category.New(
            CategoryId.New(),
            "Test Laptops",
            "Test category for laptops and notebooks"
        );

    public static CreateCategoryDto CreateTestCategoryDto() =>
        new("Test Tablets", "Test category for tablets");

    public static UpdateCategoryDto UpdateTestCategoryDto() =>
        new("Updated Category Name", "Updated category description");
}