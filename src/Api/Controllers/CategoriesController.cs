using Api.Dtos;
using Api.Modules.Errors;
using Application.Categories.Commands;
using Application.Common.Interfaces.Queries;
using Domain.Categories;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("categories")]
public class CategoriesController(ISender sender, ICategoryQueries categoryQueries) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await categoryQueries.GetAllAsync(cancellationToken);
        return categories.Select(CategoryDto.FromDomainModel).ToList();
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var category = await categoryQueries.GetByIdAsync(new CategoryId(id), cancellationToken);

        return category.Match<ActionResult<CategoryDto>>(
            c => CategoryDto.FromDomainModel(c),
            () => NotFound());
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CreateCategoryDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateCategoryCommand
        {
            Name = request.Name,
            Description = request.Description
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<CategoryDto>>(
            c => CategoryDto.FromDomainModel(c),
            e => e.ToObjectResult());
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCategoryDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryCommand
        {
            CategoryId = id,
            Name = request.Name,
            Description = request.Description
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<CategoryDto>>(
            c => CategoryDto.FromDomainModel(c),
            e => e.ToObjectResult());
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteCategoryCommand(id);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<CategoryDto>>(
            c => CategoryDto.FromDomainModel(c),
            e => e.ToObjectResult());
    }
}