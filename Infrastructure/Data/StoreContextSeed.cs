using System.Reflection;
using System.Text.Json;
using Core.Entities;
using Microsoft.AspNetCore.Identity;

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

        var products = JsonSerializer.Deserialize<List<Product>>(productsData);

        if (products == null) return;

        var existingProducts = context.Products.ToList();

        // add new ones
        if (!existingProducts.Any())
        {
            foreach (var product in products)
            {
                PopulateImage(product, path);
                context.Products.Add(product);
            }
            await context.SaveChangesAsync();
        }
        else
        {
            foreach (var product in existingProducts)
            {
                if (product.PictureData == null)
                {
                    var match = products.FirstOrDefault(x => x.Name == product.Name);
                    if (match != null)
                    {
                        product.PictureUrl = match.PictureUrl;
                        PopulateImage(product, path);
                    }
                }
            }
            await context.SaveChangesAsync();
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
}
