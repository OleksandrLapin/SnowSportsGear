using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ProductRepository(StoreContext context) : IProductRepository
{
    public void AddProduct(Product product)
    {
        context.Products.Add(product);
    }

    public void DeleteProduct(Product product)
    {
        context.Products.Remove(product);
    }

    public async Task<IReadOnlyList<string>> GetBrandsAsync()
    {
        return await context.Products.AsNoTracking().Select(x => x.Brand)
            .Distinct()
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await context.Products
            .AsNoTracking()
            .Select(p => new Product
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                PictureUrl = p.PictureUrl,
                PictureContentType = p.PictureContentType,
                Type = p.Type,
                Brand = p.Brand,
                QuantityInStock = p.QuantityInStock
            })
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(string? brand,
        string? type, string? sort)
    {
        var query = context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(brand))
            query = query.Where(x => x.Brand == brand);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(x => x.Type == type);

        query = sort switch
        {
            "priceAsc" => query.OrderBy(x => x.Price),
            "priceDesc" => query.OrderByDescending(x => x.Price),
            _ => query.OrderBy(x => x.Name)
        };

        return await query
            .Select(p => new Product
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                PictureUrl = p.PictureUrl,
                PictureContentType = p.PictureContentType,
                Type = p.Type,
                Brand = p.Brand,
                QuantityInStock = p.QuantityInStock
            })
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<Product> Data, int Count)> GetProductsPagedAsync(ProductSpecParams specParams)
    {
        var query = context.Products.AsNoTracking().AsQueryable();

        if (specParams.Brands.Any())
        {
            query = query.Where(p => specParams.Brands.Contains(p.Brand));
        }

        if (specParams.Types.Any())
        {
            query = query.Where(p => specParams.Types.Contains(p.Type));
        }

        if (!string.IsNullOrWhiteSpace(specParams.Search))
        {
            var search = $"%{specParams.Search}%";
            query = query.Where(p => EF.Functions.Like(p.Name, search));
        }

        query = specParams.Sort switch
        {
            "priceAsc" => query.OrderBy(x => x.Price),
            "priceDesc" => query.OrderByDescending(x => x.Price),
            _ => query.OrderBy(x => x.Name)
        };

        var count = await query.CountAsync();

        var data = await query
            .Skip(specParams.PageSize * (specParams.PageIndex - 1))
            .Take(specParams.PageSize)
            .Select(p => new Product
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                PictureUrl = p.PictureUrl,
                PictureContentType = p.PictureContentType,
                Type = p.Type,
                Brand = p.Brand,
                QuantityInStock = p.QuantityInStock
            })
            .ToListAsync();

        return (data, count);
    }

    public async Task<Product?> GetProductWithImageAsync(int id)
    {
        return await context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IReadOnlyList<string>> GetTypesAsync()
    {
        return await context.Products.AsNoTracking().Select(x => x.Type)
            .Distinct()
            .ToListAsync();
    }

    public bool ProductExists(int id)
    {
        return context.Products.Any(x => x.Id == id);
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }

    public void UpdateProduct(Product product)
    {
        context.Entry(product).State = EntityState.Modified;
    }
}
