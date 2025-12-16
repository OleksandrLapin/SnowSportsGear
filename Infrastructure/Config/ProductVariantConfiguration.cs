using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.Property(v => v.Size).IsRequired().HasMaxLength(10);
        builder.Property(v => v.QuantityInStock).HasDefaultValue(0);
        builder.HasIndex(v => new { v.ProductId, v.Size }).IsUnique();
    }
}
