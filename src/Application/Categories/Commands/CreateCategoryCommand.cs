using Application.Categories.Exceptions;
using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using LanguageExt;
using MediatR;

namespace Application.Categories.Commands;

public record CreateCategoryCommand : IRequest<Either<CategoryException, Category>>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}

public class CreateCategoryCommandHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<CreateCategoryCommand, Either<CategoryException, Category>>
{
    public async Task<Either<CategoryException, Category>> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var existingCategory = await categoryRepository.GetByNameAsync(request.Name, cancellationToken);

        return await existingCategory.MatchAsync(
            c => new CategoryAlreadyExistException(c.Id),
            () => CreateEntity(request, cancellationToken));
    }

    private async Task<Either<CategoryException, Category>> CreateEntity(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await categoryRepository.AddAsync(
                Category.New(CategoryId.New(), request.Name, request.Description),
                cancellationToken);

            return category;
        }
        catch (Exception exception)
        {
            return new UnhandledCategoryException(CategoryId.Empty(), exception);
        }
    }
}