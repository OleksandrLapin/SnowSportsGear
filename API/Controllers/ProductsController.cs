using API.DTOs;
using API.Extensions;
using API.RequestHelpers;
using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class ProductsController(IUnitOfWork unit) : BaseApiController
{
    [AllowAnonymous]
    [Cache(600)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts(
        [FromQuery]ProductSpecParams specParams)
    {
        var spec = new ProductSpecification(specParams);

        return await CreatePagedResult(unit.Repository<Product>(), spec, specParams.PageIndex, specParams.PageSize, p => p.ToDto());
    }

    [AllowAnonymous]
    [Cache(600)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);

        if (product == null) return NotFound();

        return product.ToDto();
    }

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromForm] CreateProductDto dto)
    {
        var product = await MapDtoToProduct(dto);

        unit.Repository<Product>().Add(product);

        if (await unit.Complete())
        {
            return CreatedAtAction("GetProduct", new { id = product.Id }, product.ToDto());
        }

        return BadRequest("Problem creating product");
    }

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromForm] CreateProductDto dto)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);

        if (product == null) return BadRequest("Cannot update this product");

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Brand = dto.Brand;
        product.Type = dto.Type;
        product.QuantityInStock = dto.QuantityInStock;

        if (dto.Image != null && dto.Image.Length > 0)
        {
            await SetImageData(product, dto.Image);
        }

        unit.Repository<Product>().Update(product);

        if (await unit.Complete())
        {
            return product.ToDto();
        }

        return BadRequest("Problem updating the product");
    }

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);

        if (product == null) return NotFound();

        unit.Repository<Product>().Remove(product);

        if (await unit.Complete())
        {
            return NoContent();
        }

        return BadRequest("Problem deleting the product");
    }

    [AllowAnonymous]
    [Cache(10000)]
    [HttpGet("brands")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetBrands()
    {
        var spec = new BrandListSpecification();

        return Ok(await unit.Repository<Product>().ListAsync(spec));
    }

    [AllowAnonymous]
    [Cache(10000)]
    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTypes()
    {
        var spec = new TypeListSpecification();

        return Ok(await unit.Repository<Product>().ListAsync(spec));
    }

    private bool ProductExists(int id) => unit.Repository<Product>().Exists(id);

    private static async Task<Product> MapDtoToProduct(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Brand = dto.Brand,
            Type = dto.Type,
            QuantityInStock = dto.QuantityInStock
        };

        await SetImageData(product, dto.Image);
        return product;
    }

    private static async Task SetImageData(Product product, IFormFile? image)
    {
        if (image == null || image.Length == 0) return;
        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);
        product.PictureData = ms.ToArray();
        product.PictureContentType = image.ContentType;
    }
}
