using Application.Common.Interfaces.Repositories;
using FluentValidation;

namespace Application.Products.Commands;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private readonly IProductRepository _productRepository; 
    
    public UpdateProductCommandValidator(IProductRepository productRepository)
    {
        _productRepository = productRepository; 

        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255)
            .MustAsync(async (command, name, cancellationToken) =>
            {
                var productOption = await _productRepository.GetByNameAsync(name, cancellationToken);
                return productOption.Match(
                    existingProduct => existingProduct.Id.Value == command.ProductId,
                    () => true
                );
            })
            .WithMessage("A product with this name already exists.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Brand)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Brand));

        RuleFor(x => x.Model)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Model));

        RuleFor(x => x.Categories)
            .NotEmpty()
            .Must(x => x.Count > 0)
            .WithMessage("At least one category is required");
    }
}