using FluentValidation;

namespace Application.Products.Commands;

public class UploadProductImagesCommandValidator : AbstractValidator<UploadProductImagesCommand>
{
    public UploadProductImagesCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        
        RuleFor(x => x.Images)
            .NotEmpty()
            .Must(x => x.Count > 0)
            .WithMessage("At least one image is required");

        RuleForEach(x => x.Images)
            .ChildRules(image =>
            {
                image.RuleFor(x => x.OriginalName).NotEmpty();
                image.RuleFor(x => x.FileStream).NotNull();
            });
    }
}