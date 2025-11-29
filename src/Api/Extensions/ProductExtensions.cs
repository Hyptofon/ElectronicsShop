using Domain.Products;

namespace Api.Extensions;

public static class ProductExtensions
{
    private const string BaseUrl = "/uploads/";

    public static string? GetPrimaryImageUrl(this Product? product)
    {
        if (product?.Images is null || product.Images.Count == 0)
            return null;

        var image = product.Images.FirstOrDefault(i => i.IsPrimary) 
                    ?? product.Images.First();

        return $"{BaseUrl}{image.GetFilePath()}";
    }
}