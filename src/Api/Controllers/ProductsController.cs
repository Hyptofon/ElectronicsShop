using Api.Dtos;
using Api.Modules.Errors;
using Application.Common.Interfaces.Queries;
using Application.Products.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("products")]
public class ProductsController(ISender sender, IProductQueries productQueries) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await productQueries.GetAllAsync(cancellationToken);
        return products.Select(ProductDto.FromDomainModel).ToList();
    }

    [AllowAnonymous]
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? categoryId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? brand,
        CancellationToken cancellationToken)
    {
        var products = await productQueries.SearchAsync(
            searchTerm,
            categoryId,
            minPrice,
            maxPrice,
            brand,
            cancellationToken);

        return products.Select(ProductDto.FromDomainModel).ToList();
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Brand = request.Brand,
            Model = request.Model,
            Categories = request.Categories
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<ProductDto>>(
            p => ProductDto.FromDomainModel(p),
            e => e.ToObjectResult());
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand
        {
            ProductId = id,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Brand = request.Brand,
            Model = request.Model,
            Categories = request.Categories
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<ProductDto>>(
            p => ProductDto.FromDomainModel(p),
            e => e.ToObjectResult());
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);
        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<ProductDto>>(
            p => ProductDto.FromDomainModel(p),
            e => e.ToObjectResult());
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost("{id:guid}/images")]
    public async Task<ActionResult<ProductDto>> UploadImages(
        [FromRoute] Guid id,
        [FromForm] IFormFileCollection? files,
        CancellationToken cancellationToken)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files uploaded");
        }

        var imageUploads = files.Select((file, index) => new ProductImageUpload
        {
            OriginalName = file.FileName,
            FileStream = file.OpenReadStream(),
            IsPrimary = index == 0
        }).ToList();

        var command = new UploadProductImagesCommand
        {
            ProductId = id,
            Images = imageUploads
        };

        var result = await sender.Send(command, cancellationToken);

        return result.Match<ActionResult<ProductDto>>(
            p => ProductDto.FromDomainModel(p),
            e => e.ToObjectResult());
    }
}