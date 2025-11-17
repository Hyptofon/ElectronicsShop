using Application.Categories.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using LanguageExt;
using MediatR;

namespace Application.Categories.Commands;

public record UpdateCategoryCommand : IRequest<Either<CategoryException, Category>>
{
    public required Guid CategoryId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public class UpdateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IApplicationDbContext dbContext)
    : IRequestHandler<UpdateCategoryCommand, Either<CategoryException, Category>>
{
    public async Task<Either<CategoryException, Category>> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId);
        var existingCategory = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);

        return await existingCategory.MatchAsync(
            category => UpdateEntity(category, request, cancellationToken),
            () => Task.FromResult<Either<CategoryException, Category>>(
                new CategoryNotFoundException(categoryId)));
    }

    private async Task<Either<CategoryException, Category>> UpdateEntity(
        Category category,
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var existingCategoryWithSameName = await categoryRepository
            .GetByNameAsync(request.Name, cancellationToken);

        if (existingCategoryWithSameName.IsSome 
            && existingCategoryWithSameName.Map(c => c.Id != category.Id).IfNone(false))
        {
            return new CategoryAlreadyExistException(category.Id);
        }
        
        try
        {
            category.UpdateDetails(request.Name, request.Description);
            categoryRepository.Update(category);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            return category;
        }
        catch (Exception exception)
        {
            return new UnhandledCategoryException(category.Id, exception);
        }
    }
}