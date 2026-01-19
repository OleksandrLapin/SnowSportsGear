using API.DTOs;
using API.Extensions;
using API.RequestHelpers;
using Core.Entities;
using Core.Helpers;
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
        // non-admin requests should not include inactive items
        var isAdmin = User.IsInRole("Admin");
        specParams.IncludeInactive = specParams.IncludeInactive && isAdmin;

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

        if (product.PictureData != null && product.PictureData.Length > 0)
        {
            var contentType = string.IsNullOrWhiteSpace(product.PictureContentType)
                ? "image/png"
                : product.PictureContentType;
            return File(product.PictureData, contentType);
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
        var salePrice = NormalizeSalePrice(dto.SalePrice, dto.Price);
        if (!TryNormalizeLowestPrice(dto.LowestPrice, dto.Price, salePrice, out var lowestPrice, out var currentLowest))
        {
            return BadRequest($"Lowest price must be less than or equal to {currentLowest:0.##}");
        }
        if (!TryNormalizeSizeGuide(dto.SizeGuide, dto.Type, out var sizeGuide, out var sizeGuideError))
        {
            return BadRequest(sizeGuideError);
        }

        var product = await MapDtoToProduct(dto, salePrice, lowestPrice, sizeGuide);

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

        var salePrice = NormalizeSalePrice(dto.SalePrice, dto.Price);
        if (!TryNormalizeLowestPrice(dto.LowestPrice, dto.Price, salePrice, out var lowestPrice, out var currentLowest))
        {
            return BadRequest($"Lowest price must be less than or equal to {currentLowest:0.##}");
        }
        if (!TryNormalizeSizeGuide(dto.SizeGuide, dto.Type, out var sizeGuide, out var sizeGuideError))
        {
            return BadRequest(sizeGuideError);
        }

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.Brand = dto.Brand;
        product.Type = dto.Type;
        product.Color = string.IsNullOrWhiteSpace(dto.Color) ? null : dto.Color.Trim();
        product.SalePrice = salePrice;
        product.LowestPrice = lowestPrice;
        product.IsActive = dto.IsActive;
        product.SizeGuide = sizeGuide;
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

    [InvalidateCache("api/products|")]
    [Authorize(Roles = "Admin")]
    [HttpPatch("{id:int}/active")]
    public async Task<ActionResult<ProductDto>> SetProductActive(int id, [FromBody] bool isActive)
    {
        var product = await productsRepo.GetProductWithVariantsAsync(id);
        if (product == null) return NotFound();

        product.IsActive = isActive;
        productsRepo.UpdateProduct(product);

        if (await productsRepo.SaveChangesAsync())
        {
            return Ok(product.ToDto(GetBaseUrl()));
        }

        return BadRequest("Problem updating product status");
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

    private static async Task<Product> MapDtoToProduct(CreateProductDto dto, decimal? salePrice, decimal lowestPrice, string? sizeGuide)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Brand = dto.Brand,
            Type = dto.Type,
            Color = string.IsNullOrWhiteSpace(dto.Color) ? null : dto.Color.Trim(),
            SalePrice = salePrice,
            LowestPrice = lowestPrice,
            IsActive = dto.IsActive,
            SizeGuide = sizeGuide,
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
        var defaultSizes = ProductSizeDefaults.GetSizesForType(product.Type);
        var allowedSizes = new HashSet<string>(defaultSizes, StringComparer.OrdinalIgnoreCase);
        var canonicalSizes = defaultSizes.ToDictionary(size => size, StringComparer.OrdinalIgnoreCase);

        var normalizedVariants = variantsDto
            .Where(v => !string.IsNullOrWhiteSpace(v.Size))
            .Select(v => new
            {
                Size = v.Size.Trim(),
                Quantity = Math.Max(v.QuantityInStock, 0)
            })
            .Where(v => !ProductSizeDefaults.IsDisallowedSize(v.Size))
            .Where(v => allowedSizes.Contains(v.Size))
            .GroupBy(v => v.Size, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Quantity, StringComparer.OrdinalIgnoreCase);

        var invalidVariants = product.Variants
            .Where(v => ProductSizeDefaults.IsDisallowedSize(v.Size) || !allowedSizes.Contains(v.Size))
            .ToList();

        foreach (var invalid in invalidVariants)
        {
            product.Variants.Remove(invalid);
        }

        var existingGroups = product.Variants
            .Where(v => !string.IsNullOrWhiteSpace(v.Size))
            .GroupBy(v => v.Size.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = new Dictionary<string, ProductVariant>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new List<ProductVariant>();
        foreach (var group in existingGroups)
        {
            var primary = group.First();
            primary.QuantityInStock = group.Sum(v => v.QuantityInStock);
            if (canonicalSizes.TryGetValue(group.Key, out var canonical))
            {
                primary.Size = canonical;
            }
            existing[group.Key] = primary;
            duplicates.AddRange(group.Skip(1));
        }

        foreach (var variant in normalizedVariants)
        {
            if (existing.TryGetValue(variant.Key, out var current))
            {
                current.QuantityInStock = variant.Value;
                existing.Remove(variant.Key);
            }
            else
            {
                var size = canonicalSizes.TryGetValue(variant.Key, out var canonical) ? canonical : variant.Key;
                product.Variants.Add(new ProductVariant
                {
                    Size = size,
                    QuantityInStock = variant.Value,
                    ProductId = product.Id
                });
            }
        }

        foreach (var stale in existing.Values.ToList())
        {
            product.Variants.Remove(stale);
        }
        foreach (var dup in duplicates)
        {
            product.Variants.Remove(dup);
        }

        var existingSizes = product.Variants.Select(v => v.Size).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var size in defaultSizes)
        {
            if (!existingSizes.Contains(size))
            {
                product.Variants.Add(new ProductVariant
                {
                    Size = size,
                    QuantityInStock = 0,
                    ProductId = product.Id
                });
            }
        }
    }
    private static decimal? NormalizeSalePrice(decimal? salePrice, decimal basePrice)
    {
        if (!salePrice.HasValue) return null;
        if (salePrice.Value <= 0) return null;
        if (salePrice.Value >= basePrice) return null;
        return Math.Round(salePrice.Value, 2);
    }

    private static decimal GetCurrentLowestPrice(decimal price, decimal? salePrice)
    {
        if (salePrice.HasValue && salePrice.Value > 0 && salePrice.Value < price)
        {
            return salePrice.Value;
        }

        return price;
    }

    private static bool TryNormalizeLowestPrice(decimal? lowestPrice, decimal price, decimal? salePrice, out decimal normalized, out decimal currentLowest)
    {
        currentLowest = GetCurrentLowestPrice(price, salePrice);
        if (!lowestPrice.HasValue || lowestPrice.Value <= 0)
        {
            normalized = Math.Round(currentLowest, 2);
            return true;
        }

        normalized = Math.Round(lowestPrice.Value, 2);
        if (normalized > currentLowest)
        {
            normalized = Math.Round(currentLowest, 2);
        }

        return true;
    }

    private static bool TryNormalizeSizeGuide(string? sizeGuideJson, string? productType, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;

        if (string.IsNullOrWhiteSpace(sizeGuideJson)) return true;

        if (!ProductSizeGuideDefaults.TryDeserialize(sizeGuideJson, out var guide) || guide == null)
        {
            error = "Size guide must be valid JSON.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(guide.Type))
        {
            guide.Type = ProductSizeGuideDefaults.NormalizeGuideType(productType) ?? string.Empty;
        }

        normalized = ProductSizeGuideDefaults.Serialize(guide);
        return true;
    }

    private string GetBaseUrl()
    {
        var request = HttpContext.Request;
        var host = request.Host.HasValue ? request.Host.Value : "localhost";
        var scheme = string.IsNullOrEmpty(request.Scheme) ? "https" : request.Scheme;
        return $"{scheme}://{host}/";
    }
}




