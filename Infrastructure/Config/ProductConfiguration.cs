using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Brand).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PictureContentType).HasMaxLength(100);
        builder.Property(x => x.PictureData).HasColumnType("varbinary(max)");

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Brand);
        builder.HasIndex(x => x.Type);
        builder.HasIndex(x => new { x.Brand, x.Type });
    }
}
