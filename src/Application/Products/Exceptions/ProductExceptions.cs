using Domain.Products;

namespace Application.Products.Exceptions;

public abstract class ProductException(ProductId productId, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ProductId ProductId { get; } = productId;
}

public class ProductAlreadyExistException(ProductId productId)
    : ProductException(productId, $"Product already exists under id {productId}");

public class ProductNotFoundException(ProductId productId)
    : ProductException(productId, $"Product not found under id {productId}");

public class ProductCategoriesNotFoundException()
    : ProductException(ProductId.Empty(), "One or more categories not found");

public class InsufficientStockException(ProductId productId, int requested, int available)
    : ProductException(productId, $"Insufficient stock for product {productId}. Requested: {requested}, Available: {available}");

public class UnhandledProductException(ProductId productId, Exception? innerException = null)
    : ProductException(productId, "Unexpected error occurred", innerException);