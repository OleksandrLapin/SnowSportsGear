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
public class ProductsController(IProductRepository productsRepo, IUnitOfWork unit) : BaseApiController
{
    [AllowAnonymous]
    [Cache(600)]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts(
        [FromQuery]ProductSpecParams specParams)
    {
        var (data, count) = await productsRepo.GetProductsPagedAsync(specParams);
        var baseUrl = GetBaseUrl();
        var pagination = new Pagination<ProductDto>(specParams.PageIndex, specParams.PageSize, count, data.Select(p => p.ToDto(baseUrl)).ToList());
        return Ok(pagination);
    }

    [AllowAnonymous]
    [Cache(600)]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var product = await productsRepo.GetProductByIdAsync(id);

        if (product == null) return NotFound();

        return product.ToDto(GetBaseUrl());
    }

    [AllowAnonymous]
    [HttpGet("{id:int}/image")]
    public async Task<IActionResult> GetProductImage(int id)
    {
        var product = await productsRepo.GetProductWithImageAsync(id);
        if (product == null) return NotFound();

        if (product.PictureData != null && product.PictureContentType != null)
        {
            return File(product.PictureData, product.PictureContentType);
        }

        if (!string.IsNullOrEmpty(product.PictureUrl))
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.PictureUrl.TrimStart('/'));
            if (System.IO.File.Exists(path))
            {
                var contentType = product.PictureContentType ?? "image/png";
                var bytes = await System.IO.File.ReadAllBytesAsync(path);
                return File(bytes, contentType);
            }
        }

        return NotFound();
    }

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromForm] CreateProductDto dto)
    {
        var product = await MapDtoToProduct(dto);

        productsRepo.AddProduct(product);

        if (await productsRepo.SaveChangesAsync())
        {
            return CreatedAtAction("GetProduct", new { id = product.Id }, product.ToDto(GetBaseUrl()));
        }

        return BadRequest("Problem creating product");
    }

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, [FromForm] CreateProductDto dto)
    {
        var product = await productsRepo.GetProductWithVariantsAsync(id);

        if (product == null) return BadRequest("Cannot update this product");

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Brand = dto.Brand;
        product.Type = dto.Type;
        ApplyVariants(product, dto.Variants);

        await SetImageData(product, dto.Image);

        productsRepo.UpdateProduct(product);

        if (await productsRepo.SaveChangesAsync())
        {
            return product.ToDto(GetBaseUrl());
        }

        return BadRequest("Problem updating the product");
    }

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await productsRepo.GetProductByIdAsync(id);

        if (product == null) return NotFound();

        productsRepo.DeleteProduct(product);

        if (await productsRepo.SaveChangesAsync())
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
        return Ok(await productsRepo.GetBrandsAsync());
    }

    [AllowAnonymous]
    [Cache(10000)]
    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetTypes()
    {
        return Ok(await productsRepo.GetTypesAsync());
    }

    private static async Task<Product> MapDtoToProduct(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Brand = dto.Brand,
            Type = dto.Type,
            Variants = []
        };

        ApplyVariants(product, dto.Variants);
        await SetImageData(product, dto.Image);
        return product;
    }

    private static async Task SetImageData(Product product, IFormFile? image)
    {
        if (image == null || image.Length == 0) return;
        product.PictureUrl = null;
        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);
        product.PictureData = ms.ToArray();
        product.PictureContentType = image.ContentType;
    }

    private static void ApplyVariants(Product product, List<CreateProductVariantDto> variantsDto)
    {
        product.Variants.Clear();
        var defaultSizes = new[] { "S", "M", "L", "XL" };

        var normalizedVariants = variantsDto
            .Where(v => !string.IsNullOrWhiteSpace(v.Size))
            .GroupBy(v => v.Size.Trim().ToUpperInvariant())
            .Select(g => new { Size = g.Key, Quantity = Math.Max(g.First().QuantityInStock, 0) });

        foreach (var variant in normalizedVariants)
        {
            product.Variants.Add(new ProductVariant
            {
                Size = variant.Size,
                QuantityInStock = variant.Quantity,
                ProductId = product.Id
            });
        }

        var existingSizes = product.Variants.Select(v => v.Size.ToUpperInvariant()).ToHashSet();
        foreach (var size in defaultSizes)
        {
            var key = size.ToUpperInvariant();
            if (!existingSizes.Contains(key))
            {
                // добавляем отсутствующие размеры с нулевым остатком, чтобы они были видны на фронте
                product.Variants.Add(new ProductVariant
                {
                    Size = key,
                    QuantityInStock = 0,
                    ProductId = product.Id
                });
            }
        }
    }

    private string GetBaseUrl()
    {
        var request = HttpContext.Request;
        var host = request.Host.HasValue ? request.Host.Value : "localhost";
        var scheme = string.IsNullOrEmpty(request.Scheme) ? "https" : request.Scheme;
        return $"{scheme}://{host}/";
    }
}
