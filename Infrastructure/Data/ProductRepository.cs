using Core.Entities;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(string? brand,
        string? type, string? sort)
    {
        var query = context.Products.AsNoTracking().AsQueryable();
        Expression<Func<Product, decimal>> actualPrice = x =>
            x.SalePrice.HasValue && x.SalePrice.Value > 0 && x.SalePrice.Value < x.Price
                ? x.SalePrice.Value
                : x.Price;

        if (!string.IsNullOrWhiteSpace(brand))
            query = query.Where(x => x.Brand == brand);

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(x => x.Type == type);

        query = sort switch
        {
            "priceAsc" => query.OrderBy(actualPrice),
            "priceDesc" => query.OrderByDescending(actualPrice),
            "ratingDesc" => query.OrderByDescending(x => x.RatingAverage),
            "ratingAsc" => query.OrderBy(x => x.RatingAverage),
            _ => query.OrderBy(x => x.Name)
        };

        return await query
            .Include(p => p.Variants)
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<Product> Data, int Count)> GetProductsPagedAsync(ProductSpecParams specParams)
    {
        var query = context.Products.AsNoTracking().AsQueryable();
        Expression<Func<Product, decimal>> actualPrice = x =>
            x.SalePrice.HasValue && x.SalePrice.Value > 0 && x.SalePrice.Value < x.Price
                ? x.SalePrice.Value
                : x.Price;

        if (!specParams.IncludeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

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
            "priceAsc" => query.OrderBy(actualPrice),
            "priceDesc" => query.OrderByDescending(actualPrice),
            "ratingDesc" => query.OrderByDescending(x => x.RatingAverage),
            "ratingAsc" => query.OrderBy(x => x.RatingAverage),
            _ => query.OrderBy(x => x.Name)
        };

        var count = await query.CountAsync();

        var data = await query
            .Include(p => p.Variants)
            .Skip(specParams.PageSize * (specParams.PageIndex - 1))
            .Take(specParams.PageSize)
            .ToListAsync();

        return (data, count);
    }

    public async Task<Product?> GetProductWithImageAsync(int id)
    {
        return await context.Products.AsNoTracking()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Product?> GetProductWithVariantsAsync(int id)
    {
        return await context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(x => x.Id == id);
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
