using Core.Entities;

using Core.Specifications;

namespace Core.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetProductsAsync(string? brand, string? type, string? sort);
    Task<Product?> GetProductByIdAsync(int id);
    Task<(IReadOnlyList<Product> Data, int Count)> GetProductsPagedAsync(ProductSpecParams specParams);
    Task<Product?> GetProductWithImageAsync(int id);
    Task<Product?> GetProductWithVariantsAsync(int id);
    Task<IReadOnlyList<string>> GetBrandsAsync();
    Task<IReadOnlyList<string>> GetTypesAsync();
    void AddProduct(Product product);
    void UpdateProduct(Product product);
    void DeleteProduct(Product product);
    bool ProductExists(int id);
    Task<bool> SaveChangesAsync();
}
