using System.Reflection;
using System.Text.Json;
using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class StoreContextSeed
{
    public static async Task SeedAsync(StoreContext context, UserManager<AppUser> userManager)
    {
        var adminUser = await userManager.FindByEmailAsync("admin@test.com");

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = "admin@test.com",
                Email = "admin@test.com",
            };

            await userManager.CreateAsync(adminUser, "Pa$$w0rd");
        }

        var adminRoles = await userManager.GetRolesAsync(adminUser);
        if (!adminRoles.Contains("Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        var productsData = await File
            .ReadAllTextAsync(path + @"/Data/SeedData/products.json");

        var products = JsonSerializer.Deserialize<List<SeedProduct>>(productsData);

        if (products == null) return;

        var hasProducts = await context.Products.AnyAsync();

        if (!hasProducts)
        {
            foreach (var seedProduct in products)
            {
                var product = seedProduct.ToProduct();
                PopulateImage(product, path);
                product.Variants = BuildDefaultVariants();
                context.Products.Add(product);
            }
            await context.SaveChangesAsync();
        }
        else
        {
            var productsMissingImages = await context.Products
                .Include(p => p.Variants)
                .Where(p => p.PictureData == null)
                .ToListAsync();

            foreach (var product in productsMissingImages)
            {
                if (product.PictureData == null)
                {
                    var match = products.FirstOrDefault(x => x.Name == product.Name);
                    if (match != null)
                    {
                        product.PictureUrl = match.PictureUrl;
                        PopulateImage(product, path);
                        if ((product.Variants == null || !product.Variants.Any()))
                        {
                            product.Variants = BuildDefaultVariants();
                        }
                    }
                }
            }

            if (productsMissingImages.Count > 0)
            {
                await context.SaveChangesAsync();
            }
        }

        if (!context.DeliveryMethods.Any())
        {
            var dmData = await File
                .ReadAllTextAsync(path + @"/Data/SeedData/delivery.json");

            var methods = JsonSerializer.Deserialize<List<DeliveryMethod>>(dmData);

            if (methods == null) return;

            context.DeliveryMethods.AddRange(methods);

            await context.SaveChangesAsync();
        }
    }

    private static void PopulateImage(Product product, string rootPath)
    {
        if (product.PictureData != null) return;
        var relativePath = product.PictureUrl?.TrimStart('/') ?? string.Empty;
        if (string.IsNullOrEmpty(relativePath)) return;

        var imagePath = Path.Combine(rootPath ?? string.Empty, "wwwroot", relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!File.Exists(imagePath)) return;

        product.PictureData = File.ReadAllBytes(imagePath);
        product.PictureContentType = GetContentType(imagePath);
    }

    private static string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }

    private static List<ProductVariant> BuildDefaultVariants()
    {
        return new List<ProductVariant>
        {
            new() { Size = "S", QuantityInStock = 5 },
            new() { Size = "M", QuantityInStock = 7 },
            new() { Size = "L", QuantityInStock = 10 },
            new() { Size = "XL", QuantityInStock = 12 },
        };
    }

    private class SeedProduct
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string PictureUrl { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public int QuantityInStock { get; set; }

        public Product ToProduct()
        {
            return new Product
            {
                Name = Name,
                Description = Description,
                Price = Price,
                PictureUrl = PictureUrl,
                Type = Type,
                Brand = Brand,
                Variants = []
            };
        }
    }
}
