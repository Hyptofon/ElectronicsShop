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

public class ProductCategoriesNotLoadedException(ProductId productId)
    : ProductException(productId, "Categories not loaded");

public class InsufficientStockException(ProductId productId, int requested, int available)
    : ProductException(productId, $"Insufficient stock for product {productId}. Requested: {requested}, Available: {available}");

public class UnhandledProductException(ProductId productId, Exception? innerException = null)
    : ProductException(productId, "Unexpected error occurred", innerException);
    
public class ProductImageNotFoundException(ProductImageId imageId)
    : ProductException(ProductId.Empty(), $"Product image not found under id {imageId.Value}");
public class ProductCannotBeDeletedDueToOrdersException(ProductId productId)
    : ProductException(productId, $"Product {productId} cannot be deleted because it is referenced in existing orders");

public class ProductCannotBeDeletedDueToCartsException(ProductId productId)
    : ProductException(productId, $"Product {productId} cannot be deleted because it is referenced in user shopping carts");

public class ProductCannotBeDeletedDueToReviewsException(ProductId productId)
    : ProductException(productId, $"Product {productId} cannot be deleted because it has associated product reviews");