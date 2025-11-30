using Application.Categories.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using LanguageExt;
using MediatR;

namespace Application.Categories.Commands;

public record DeleteCategoryCommand(Guid CategoryId) 
    : IRequest<Either<CategoryException, Category>>;

public class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IApplicationDbContext dbContext)
    : IRequestHandler<DeleteCategoryCommand, Either<CategoryException, Category>>
{
    public async Task<Either<CategoryException, Category>> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var categoryId = new CategoryId(request.CategoryId);
        
        var hasProducts = await categoryRepository.HasProductsAsync(categoryId, cancellationToken);
        if (hasProducts)
        {
            return new CategoryCannotBeDeletedException(categoryId);
        }
        
        var existingCategory = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);

        return await existingCategory.MatchAsync(
            category => DeleteEntity(category, cancellationToken),
            () => Task.FromResult<Either<CategoryException, Category>>(
                new CategoryNotFoundException(categoryId)));
    }

    private async Task<Either<CategoryException, Category>> DeleteEntity(
        Category category,
        CancellationToken cancellationToken)
    {
        try
        {
            categoryRepository.Delete(category);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            return category;
        }
        catch (Exception exception)
        {
            return new UnhandledCategoryException(category.Id, exception);
        }
    }
}