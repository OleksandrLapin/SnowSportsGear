using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class SecurityCodeConfiguration : IEntityTypeConfiguration<SecurityCode>
{
    public void Configure(EntityTypeBuilder<SecurityCode> builder)
    {
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.Purpose).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CodeSalt).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Token).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.TargetEmail).HasMaxLength(256);
        builder.HasIndex(x => new { x.UserId, x.Purpose });
        builder.HasIndex(x => x.ExpiresAt);
    }
}
