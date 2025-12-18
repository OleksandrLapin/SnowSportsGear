using System.Reflection;
using System.Text.Json;
using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class StoreContextSeed
{
    private const string DefaultPassword = "Pa$$w0rd";

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

            await userManager.CreateAsync(adminUser, DefaultPassword);
        }

        var adminRoles = await userManager.GetRolesAsync(adminUser);
        if (!adminRoles.Contains("Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        var sampleUsers = await EnsureSampleUsers(userManager);

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

        var productsInDb = await context.Products.ToListAsync();

        if (!await context.ProductReviews.AnyAsync())
        {
            var random = new Random(42);
            var adminResponderEmail = adminUser.Email ?? "admin@test.com";

            foreach (var product in productsInDb)
            {
                var reviewsToCreate = Math.Min(random.Next(0, 16), sampleUsers.Count);
                var usedUserIds = new HashSet<string>();

                for (int i = 0; i < reviewsToCreate; i++)
                {
                    var reviewer = sampleUsers[random.Next(sampleUsers.Count)];
                    if (!usedUserIds.Add(reviewer.Id))
                    {
                        i--;
                        continue;
                    }

                    var rating = random.Next(1, 6);
                    var createdAt = DateTime.UtcNow.AddDays(-random.Next(10, 220));
                    var isHidden = random.NextDouble() < 0.1;
                    var title = random.NextDouble() > 0.6 ? GetRandomTitle(random) : null;
                    var content = random.NextDouble() > 0.35 ? GetRandomBody(random) : null;

                    var review = new ProductReview
                    {
                        ProductId = product.Id,
                        UserId = reviewer.Id,
                        Rating = rating,
                        Title = title,
                        Content = content,
                        CreatedAt = createdAt,
                        UpdatedAt = createdAt,
                        IsHidden = isHidden
                    };

                    if (random.NextDouble() < 0.25)
                    {
                        review.AdminResponse = GetAdminResponse(random);
                        review.AdminResponderId = adminUser.Id;
                        review.AdminResponderEmail = adminResponderEmail;
                        review.AdminRespondedAt = createdAt.AddDays(random.Next(1, 30));
                    }

                    context.ProductReviews.Add(review);
                    context.ReviewAudits.Add(new ReviewAudit
                    {
                        Review = review,
                        Action = ReviewAction.Created,
                        ActorUserId = reviewer.Id,
                        ActorEmail = reviewer.Email ?? string.Empty,
                        CreatedAt = createdAt,
                        NewRating = rating,
                        NewTitle = title,
                        NewContent = content,
                        OldHidden = isHidden,
                        NewHidden = isHidden,
                        NewResponse = review.AdminResponse
                    });
                }
            }

            await context.SaveChangesAsync();

            var reviewStats = await context.ProductReviews
                .Where(r => !r.IsHidden)
                .GroupBy(r => r.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Average = g.Average(r => r.Rating),
                    Count = g.Count()
                })
                .ToListAsync();

            foreach (var product in productsInDb)
            {
                var stats = reviewStats.FirstOrDefault(s => s.ProductId == product.Id);
                product.RatingAverage = stats?.Average ?? 0;
                product.RatingCount = stats?.Count ?? 0;
                context.Products.Update(product);
            }

            await context.SaveChangesAsync();
        }

        await ApplyPricingMetaAsync(context, productsInDb);

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

    private static async Task ApplyPricingMetaAsync(StoreContext context, List<Product> productsInDb)
    {
        if (productsInDb.Count == 0) return;

        var random = new Random(123);
        var needsSave = false;

        // ensure lowest national price for all products
        foreach (var product in productsInDb)
        {
            if (!product.LowestPrice.HasValue || product.LowestPrice <= 0)
            {
                // set between 85% and 95% of current price
                var factor = 0.85m + (decimal)(random.NextDouble() * 0.1);
                product.LowestPrice = Math.Round(product.Price * factor, 2);
                needsSave = true;
            }
        }

        // apply promo price to ~10% of products (only if not already on sale)
        var saleTargetCount = Math.Max(1, (int)Math.Ceiling(productsInDb.Count * 0.1));
        var shuffled = productsInDb.OrderBy(_ => random.Next()).ToList();
        var selectedForSale = shuffled.Take(saleTargetCount);

        foreach (var product in selectedForSale)
        {
            if (product.SalePrice.HasValue && product.SalePrice > 0 && product.SalePrice < product.Price)
            {
                continue;
            }

            var discountFactor = 0.8m + (decimal)(random.NextDouble() * 0.1); // 10-20% off
            var salePrice = Math.Round(product.Price * discountFactor, 2);

            if (salePrice >= product.Price) continue;

            product.SalePrice = salePrice;
            needsSave = true;
        }

        if (needsSave)
        {
            await context.SaveChangesAsync();
        }
    }

    private static async Task<List<AppUser>> EnsureSampleUsers(UserManager<AppUser> userManager)
    {
        var samples = new (string Email, string FirstName, string LastName)[]
        {
            ("clara@test.com", "Clara", "Frost"),
            ("felix@test.com", "Felix", "Summers"),
            ("nina@test.com", "Nina", "Cloud"),
            ("liam@test.com", "Liam", "Baker"),
            ("mia@test.com", "Mia", "Stone"),
            ("oscar@test.com", "Oscar", "Bright"),
            ("ivy@test.com", "Ivy", "Park"),
            ("jake@test.com", "Jake", "Forest"),
            ("amelia@test.com", "Amelia", "Ray"),
            ("ethan@test.com", "Ethan", "Vale"),
            ("zoe@test.com", "Zoe", "Barnes"),
            ("noah@test.com", "Noah", "Reed"),
            ("layla@test.com", "Layla", "Snow"),
            ("henry@test.com", "Henry", "Cross"),
            ("sophia@test.com", "Sophia", "Lake"),
            ("dylan@test.com", "Dylan", "North"),
            ("ruby@test.com", "Ruby", "Cliff"),
            ("lucas@test.com", "Lucas", "Fjord")
        };

        var users = new List<AppUser>();

        foreach (var sample in samples)
        {
            var user = await userManager.FindByEmailAsync(sample.Email);
            if (user == null)
            {
                user = new AppUser
                {
                    UserName = sample.Email,
                    Email = sample.Email,
                    FirstName = sample.FirstName,
                    LastName = sample.LastName
                };

                await userManager.CreateAsync(user, DefaultPassword);
            }

            users.Add(user);
        }

        return users;
    }

    private static string GetRandomTitle(Random random)
    {
        var titles = new[]
        {
            "Perfect for the slopes",
            "Solid quality gear",
            "Comfortable and warm",
            "Great value",
            "Met my expectations",
            "Could be better",
            "Love the fit",
            "Reliable so far",
            "Exactly as described",
            "Impressive build quality"
        };

        return titles[random.Next(titles.Length)];
    }

    private static string GetRandomBody(Random random)
    {
        var bodies = new[]
        {
            "Used it for a week-long trip and it performed flawlessly.",
            "Materials feel durable and it kept me warm in windy conditions.",
            "Sizing runs true. I appreciate the attention to detail on the seams.",
            "The color looks even better in person. Would buy again.",
            "Took a couple of runs to break in, but now it's perfect.",
            "Lightweight yet sturdy. Handles moisture really well.",
            "A bit snug with layers, but overall comfort is great.",
            "Shipping was quick and the packaging was solid.",
            "Price is fair for the quality you get here.",
            "Happy with the purchase; it matches the product photos."
        };

        return bodies[random.Next(bodies.Length)];
    }

    private static string GetAdminResponse(Random random)
    {
        var responses = new[]
        {
            "Thanks for sharing your feedback with us!",
            "We're glad it worked well for you.",
            "Appreciate the note—enjoy your time on the slopes.",
            "Thanks for the review. Let us know if you need any help.",
            "Happy to hear the fit worked out for you."
        };

        return responses[random.Next(responses.Length)];
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
