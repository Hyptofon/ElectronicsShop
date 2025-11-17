using Application.Common.Interfaces.Repositories; 
using FluentValidation;

namespace Application.Products.Commands;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private readonly IProductRepository _productRepository; 
    
    public CreateProductCommandValidator(IProductRepository productRepository) 
    {
        _productRepository = productRepository; 
        
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255)
            .MustAsync(async (name, cancellationToken) =>
            {
                var productOption = await _productRepository.GetByNameAsync(name, cancellationToken);
                return productOption.IsNone;
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