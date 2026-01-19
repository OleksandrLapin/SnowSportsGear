using System.Reflection;
using System.Data;
using System.Text.Json;
using Core.Constants;
using Core.Entities;
using Core.Helpers;
using Core.Entities.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class StoreContextSeed
{
    private const string DefaultPassword = "Pa$$w0rd";
    private const string AdminEmail = "snowsportsgearnoreply@gmail.com";
    private const string LegacyAdminEmail = "admin@test.com";

    public static async Task SeedAsync(StoreContext context, UserManager<AppUser> userManager)
    {
        var adminUser = await userManager.FindByEmailAsync(AdminEmail);
        if (adminUser == null)
        {
            adminUser = await userManager.FindByEmailAsync(LegacyAdminEmail);
            if (adminUser != null)
            {
                adminUser.UserName = AdminEmail;
                adminUser.Email = AdminEmail;
                adminUser.EmailConfirmed = true;
                adminUser.TwoFactorEnabled = true;
                await userManager.UpdateAsync(adminUser);
            }
        }

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
                TwoFactorEnabled = true
            };

            await userManager.CreateAsync(adminUser, DefaultPassword);
        }
        else if (!adminUser.EmailConfirmed || !adminUser.TwoFactorEnabled)
        {
            adminUser.EmailConfirmed = true;
            adminUser.TwoFactorEnabled = true;
            await userManager.UpdateAsync(adminUser);
        }

        var adminRoles = await userManager.GetRolesAsync(adminUser);
        if (!adminRoles.Contains("Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        var sampleUsers = await EnsureSampleUsers(userManager);
        await EnsureOrderColumnsAsync(context);

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
                product.Variants = BuildDefaultVariants(product.Type);
                product.SizeGuide = ProductSizeGuideDefaults.Serialize(
                    ProductSizeGuideDefaults.GetDefaultForType(product.Type));
                context.Products.Add(product);
            }
            await context.SaveChangesAsync();
        }
        else
        {
            var productsMissingImages = (await context.Products
                .Include(p => p.Variants)
                .ToListAsync())
                .Where(p =>
                    p.PictureData == null ||
                    p.PictureData.Length == 0 ||
                    string.IsNullOrWhiteSpace(p.PictureUrl))
                .ToList();

            foreach (var product in productsMissingImages)
            {
                if (product.PictureData == null || product.PictureData.Length == 0)
                {
                    var match = products.FirstOrDefault(x => x.Name == product.Name);
                    if (match != null)
                    {
                        product.PictureUrl = match.PictureUrl;
                        PopulateImage(product, path);
                        if ((product.Variants == null || !product.Variants.Any()))
                        {
                            product.Variants = BuildDefaultVariants(product.Type);
                        }
                    }
                }
            }

            if (productsMissingImages.Count > 0)
            {
                await context.SaveChangesAsync();
            }
        }

        var productsInDb = await context.Products
            .Include(p => p.Variants)
            .ToListAsync();

        var variantsUpdated = false;
        var descriptionsUpdated = false;
        var sizeGuidesUpdated = false;
        var seedByName = products.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var product in productsInDb)
        {
            if (EnsureProductVariants(product))
            {
                variantsUpdated = true;
            }

            if (seedByName.TryGetValue(product.Name, out var seed) &&
                !string.Equals(product.Description, seed.Description, StringComparison.Ordinal))
            {
                product.Description = seed.Description;
                descriptionsUpdated = true;
            }

            if (EnsureProductSizeGuide(product))
            {
                sizeGuidesUpdated = true;
            }
        }

        if (variantsUpdated || descriptionsUpdated || sizeGuidesUpdated)
        {
            await context.SaveChangesAsync();
        }

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

        await EnsureNotificationTemplatesAsync(context);
    }

    private static async Task EnsureNotificationTemplatesAsync(StoreContext context)
    {
        await EnsureNotificationTablesAsync(context);

        if (!await context.NotificationTemplates.AnyAsync())
        {
            context.NotificationTemplates.AddRange(NotificationTemplateDefaults.GetDefaults());
            await context.SaveChangesAsync();
            return;
        }

        var existingKeys = await context.NotificationTemplates
            .Select(t => t.Key)
            .ToListAsync();

        var existingSet = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);
        var toAdd = NotificationTemplateDefaults.GetDefaults()
            .Where(t => !existingSet.Contains(t.Key))
            .ToList();

        if (toAdd.Count > 0)
        {
            context.NotificationTemplates.AddRange(toAdd);
            await context.SaveChangesAsync();
        }

        var codeTemplateKeys = new[]
        {
            NotificationTemplateKeys.AccountEmailConfirmation,
            NotificationTemplateKeys.AccountPasswordReset,
            NotificationTemplateKeys.AccountEmailChange,
            NotificationTemplateKeys.AccountTwoFactorCode
        };

        var templatesToUpdate = (await context.NotificationTemplates
            .Where(t => codeTemplateKeys.Contains(t.Key))
            .ToListAsync())
            .Where(t => t.Body.Contains("<strong>{{Code}}</strong>", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (templatesToUpdate.Count > 0)
        {
            var updatedAt = DateTime.UtcNow;
            foreach (var template in templatesToUpdate)
            {
                template.Body = template.Body.Replace(
                    "<strong>{{Code}}</strong>",
                    "<span class=\"code\">{{Code}}</span>",
                    StringComparison.OrdinalIgnoreCase);
                template.UpdatedAt = updatedAt;
            }
            await context.SaveChangesAsync();
        }

        var codeStyle = "display:inline-block; margin:12px 0; padding:10px 14px; font-family:'Courier New', monospace; font-size:18px; letter-spacing:2px; color:#4c1d95; background:#f3e8ff; border:1px dashed #c4b5fd; border-radius:10px;";
        var templatesWithCodeSpan = (await context.NotificationTemplates
            .Where(t => codeTemplateKeys.Contains(t.Key))
            .ToListAsync())
            .Where(t => t.Body.Contains("class=\"code\"", StringComparison.OrdinalIgnoreCase) ||
                        t.Body.Contains("class='code'", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (templatesWithCodeSpan.Count > 0)
        {
            var updatedAt = DateTime.UtcNow;
            foreach (var template in templatesWithCodeSpan)
            {
                template.Body = template.Body
                    .Replace("<span class=\"code\">", $"<span style=\"{codeStyle}\">", StringComparison.OrdinalIgnoreCase)
                    .Replace("<span class='code'>", $"<span style=\"{codeStyle}\">", StringComparison.OrdinalIgnoreCase);
                template.UpdatedAt = updatedAt;
            }
            await context.SaveChangesAsync();
        }

        var orderTemplate = await context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Key == NotificationTemplateKeys.OrderCreated);
        if (orderTemplate != null && !orderTemplate.Body.Contains("margin:16px 0", StringComparison.OrdinalIgnoreCase))
        {
            orderTemplate.Body = "We received your order and will start processing it soon." +
                "<div style=\"margin:16px 0;\">{{OrderSummary}}</div>" +
                "<div style=\"margin:8px 0;\">Ship to: {{ShippingAddress}}</div>" +
                "<div style=\"margin:8px 0;\">Payment: {{PaymentSummary}}</div>";
            orderTemplate.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureNotificationTablesAsync(StoreContext context)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = @"
IF OBJECT_ID(N'[SecurityCodes]', N'U') IS NULL
BEGIN
    CREATE TABLE [SecurityCodes] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Purpose] nvarchar(100) NOT NULL,
        [CodeHash] nvarchar(128) NOT NULL,
        [CodeSalt] nvarchar(64) NOT NULL,
        [Token] nvarchar(max) NOT NULL,
        [TargetEmail] nvarchar(256) NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UsedAt] datetime2 NULL,
        CONSTRAINT [PK_SecurityCodes] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_SecurityCodes_UserId_Purpose] ON [SecurityCodes] ([UserId], [Purpose]);
    CREATE INDEX [IX_SecurityCodes_ExpiresAt] ON [SecurityCodes] ([ExpiresAt]);
END;

IF OBJECT_ID(N'[NotificationTemplates]', N'U') IS NULL
BEGIN
    CREATE TABLE [NotificationTemplates] (
        [Id] int NOT NULL IDENTITY,
        [Key] nvarchar(200) NOT NULL,
        [Category] int NOT NULL,
        [Channel] int NOT NULL,
        [Subject] nvarchar(200) NOT NULL,
        [Headline] nvarchar(200) NOT NULL,
        [Body] nvarchar(4000) NOT NULL,
        [CtaLabel] nvarchar(120) NULL,
        [CtaUrl] nvarchar(1000) NULL,
        [Footer] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_NotificationTemplates] PRIMARY KEY ([Id])
    );
    CREATE UNIQUE INDEX [IX_NotificationTemplates_Key] ON [NotificationTemplates] ([Key]);
END;

IF OBJECT_ID(N'[NotificationMessages]', N'U') IS NULL
BEGIN
    CREATE TABLE [NotificationMessages] (
        [Id] int NOT NULL IDENTITY,
        [TemplateKey] nvarchar(200) NOT NULL,
        [Channel] int NOT NULL,
        [RecipientEmail] nvarchar(256) NOT NULL,
        [RecipientUserId] nvarchar(max) NULL,
        [Subject] nvarchar(200) NOT NULL,
        [HtmlBody] nvarchar(max) NOT NULL,
        [TextBody] nvarchar(4000) NULL,
        [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [SentAt] datetime2 NULL,
        [Error] nvarchar(2000) NULL,
        [Metadata] nvarchar(4000) NULL,
        CONSTRAINT [PK_NotificationMessages] PRIMARY KEY ([Id])
    );
    CREATE INDEX [IX_NotificationMessages_TemplateKey] ON [NotificationMessages] ([TemplateKey]);
    CREATE INDEX [IX_NotificationMessages_RecipientEmail] ON [NotificationMessages] ([RecipientEmail]);
    CREATE INDEX [IX_NotificationMessages_Status] ON [NotificationMessages] ([Status]);
END;";
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task EnsureOrderColumnsAsync(StoreContext context)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = @"
IF COL_LENGTH('Orders', 'StatusUpdatedAt') IS NULL
    ALTER TABLE [Orders] ADD [StatusUpdatedAt] datetime2 NULL;
IF COL_LENGTH('Orders', 'TrackingNumber') IS NULL
    ALTER TABLE [Orders] ADD [TrackingNumber] nvarchar(200) NULL;
IF COL_LENGTH('Orders', 'TrackingUrl') IS NULL
    ALTER TABLE [Orders] ADD [TrackingUrl] nvarchar(1000) NULL;
IF COL_LENGTH('Orders', 'CancelledBy') IS NULL
    ALTER TABLE [Orders] ADD [CancelledBy] nvarchar(200) NULL;
IF COL_LENGTH('Orders', 'CancelledReason') IS NULL
    ALTER TABLE [Orders] ADD [CancelledReason] nvarchar(1000) NULL;
IF COL_LENGTH('Orders', 'DeliveryUpdateDetails') IS NULL
    ALTER TABLE [Orders] ADD [DeliveryUpdateDetails] nvarchar(2000) NULL;";
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void PopulateImage(Product product, string? rootPath)
    {
        if (product.PictureData != null) return;
        var relativePath = product.PictureUrl?.TrimStart('/', '\\') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath)) return;

        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        foreach (var basePath in GetImageSearchRoots(rootPath))
        {
            if (!Directory.Exists(basePath)) continue;

            var imagePath = Path.Combine(basePath, normalizedPath);
            if (!File.Exists(imagePath)) continue;

            product.PictureData = File.ReadAllBytes(imagePath);
            product.PictureContentType = GetContentType(imagePath);
            return;
        }
    }

    private static IEnumerable<string> GetImageSearchRoots(string? rootPath)
    {
        var roots = new List<string>();

        if (!string.IsNullOrWhiteSpace(rootPath))
        {
            roots.Add(Path.Combine(rootPath, "wwwroot"));
        }

        var current = Directory.GetCurrentDirectory();
        roots.Add(Path.Combine(current, "wwwroot"));
        roots.Add(Path.Combine(current, "..", "client", "public"));

        var repoRoot = FindRepoRoot(current);
        if (!string.IsNullOrWhiteSpace(repoRoot))
        {
            roots.Add(Path.Combine(repoRoot, "client", "public"));
            roots.Add(Path.Combine(repoRoot, "API", "wwwroot"));
        }

        roots.Add(Path.Combine(AppContext.BaseDirectory, "wwwroot"));

        return roots
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? FindRepoRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SnowSportsGear.sln")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        return null;
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

    private static List<ProductVariant> BuildDefaultVariants(string? type)
    {
        var sizes = ProductSizeDefaults.GetSizesForType(type);
        return BuildDefaultVariants(sizes);
    }

    private static List<ProductVariant> BuildDefaultVariants(IReadOnlyList<string> sizes)
    {
        var variants = new List<ProductVariant>(sizes.Count);
        for (var i = 0; i < sizes.Count; i++)
        {
            variants.Add(new ProductVariant
            {
                Size = sizes[i],
                QuantityInStock = GetDefaultQuantityForSize(sizes[i], i)
            });
        }

        return variants;
    }

    private static int GetDefaultQuantityForSize(string size, int index)
    {
        return size.ToUpperInvariant() switch
        {
            "S" => 5,
            "M" => 7,
            "L" => 10,
            "XL" => 12,
            _ => 5 + (index * 2)
        };
    }

    private static bool EnsureProductVariants(Product product)
    {
        var defaultSizes = ProductSizeDefaults.GetSizesForType(product.Type);
        var allowedSizes = new HashSet<string>(defaultSizes, StringComparer.OrdinalIgnoreCase);

        var existingVariants = product.Variants?.ToList() ?? [];
        var existingTotal = existingVariants
            .Where(v => !ProductSizeDefaults.IsDisallowedSize(v.Size))
            .Sum(v => v.QuantityInStock);

        var sizeQuantities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var hasInvalid = false;

        foreach (var variant in existingVariants)
        {
            if (ProductSizeDefaults.IsDisallowedSize(variant.Size))
            {
                hasInvalid = true;
                continue;
            }

            var normalized = variant.Size.Trim();
            if (!allowedSizes.Contains(normalized))
            {
                hasInvalid = true;
                continue;
            }

            if (sizeQuantities.TryGetValue(normalized, out var current))
            {
                sizeQuantities[normalized] = current + variant.QuantityInStock;
            }
            else
            {
                sizeQuantities[normalized] = variant.QuantityInStock;
            }
        }

        var desiredVariants = defaultSizes
            .Select(size => new ProductVariant
            {
                Size = size,
                QuantityInStock = sizeQuantities.TryGetValue(size, out var quantity) ? quantity : 0,
                ProductId = product.Id
            })
            .ToList();

        if (sizeQuantities.Count == 0 && existingTotal > 0)
        {
            DistributeQuantity(desiredVariants, existingTotal);
        }

        var needsUpdate = hasInvalid || existingVariants.Count != desiredVariants.Count;
        if (!needsUpdate)
        {
            foreach (var desired in desiredVariants)
            {
                var match = existingVariants.FirstOrDefault(v => string.Equals(v.Size.Trim(), desired.Size, StringComparison.OrdinalIgnoreCase));
                if (match == null || match.QuantityInStock != desired.QuantityInStock)
                {
                    needsUpdate = true;
                    break;
                }
            }
        }

        if (!needsUpdate)
        {
            return false;
        }

        product.Variants.Clear();
        foreach (var variant in desiredVariants)
        {
            product.Variants.Add(variant);
        }

        return true;
    }

    private static bool EnsureProductSizeGuide(Product product)
    {
        if (!string.IsNullOrWhiteSpace(product.SizeGuide)) return false;

        var guide = ProductSizeGuideDefaults.GetDefaultForType(product.Type);
        if (guide == null) return false;

        product.SizeGuide = ProductSizeGuideDefaults.Serialize(guide);
        return true;
    }

    private static void DistributeQuantity(List<ProductVariant> variants, int totalQuantity)
    {
        if (variants.Count == 0) return;

        var perSize = totalQuantity / variants.Count;
        var remainder = totalQuantity % variants.Count;

        for (var i = 0; i < variants.Count; i++)
        {
            variants[i].QuantityInStock = perSize + (i < remainder ? 1 : 0);
        }
    }

    private static async Task ApplyPricingMetaAsync(StoreContext context, List<Product> productsInDb)
    {
        if (productsInDb.Count == 0) return;

        var random = new Random(123);
        var needsSave = false;

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

        foreach (var product in productsInDb)
        {
            var currentLowest = GetCurrentLowestPrice(product.Price, product.SalePrice);
            if (!product.LowestPrice.HasValue || product.LowestPrice <= 0 || product.LowestPrice > currentLowest)
            {
                product.LowestPrice = Math.Round(currentLowest, 2);
                needsSave = true;
            }
        }

        if (needsSave)
        {
            await context.SaveChangesAsync();
        }
    }

    private static decimal GetCurrentLowestPrice(decimal price, decimal? salePrice)
    {
        if (salePrice.HasValue && salePrice.Value > 0 && salePrice.Value < price)
        {
            return salePrice.Value;
        }

        return price;
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
                    LastName = sample.LastName,
                    EmailConfirmed = true,
                    TwoFactorEnabled = true
                };

                await userManager.CreateAsync(user, DefaultPassword);
            }
            else if (!user.EmailConfirmed || !user.TwoFactorEnabled)
            {
                user.EmailConfirmed = true;
                user.TwoFactorEnabled = true;
                await userManager.UpdateAsync(user);
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





