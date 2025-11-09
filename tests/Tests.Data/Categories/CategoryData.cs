using Api.Dtos;
using Domain.Categories;

namespace Tests.Data.Categories;

public static class CategoryData
{
    // ВИПРАВЛЕННЯ: Додаємо timestamp для унікальності
    public static Category FirstTestCategory(string prefix = "Test") 
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];
        return Category.New(
            CategoryId.New(),
            $"{prefix}-{uniqueId}-Smartphones",
            $"{prefix} category for smartphones and mobile devices"
        );
    }

    public static Category SecondTestCategory(string prefix = "Test") 
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];
        return Category.New(
            CategoryId.New(),
            $"{prefix}-{uniqueId}-Laptops",
            $"{prefix} category for laptops and notebooks"
        );
    }

    public static CreateCategoryDto CreateTestCategoryDto(string prefix = "Test")
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];
        return new CreateCategoryDto(
            $"{prefix}-{uniqueId}-Tablets", 
            $"{prefix} category for tablets"
        );
    }

    public static UpdateCategoryDto UpdateTestCategoryDto(string prefix = "Updated")
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];
        return new UpdateCategoryDto(
            $"{prefix}-{uniqueId}-Category", 
            $"{prefix} category description"
        );
    }
}