using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class ReviewAuditConfiguration : IEntityTypeConfiguration<ReviewAudit>
{
    public void Configure(EntityTypeBuilder<ReviewAudit> builder)
    {
        builder.Property(a => a.Action).IsRequired();
        builder.Property(a => a.ActorUserId).HasMaxLength(450);
        builder.Property(a => a.ActorEmail).HasMaxLength(256);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.OldResponse).HasMaxLength(1000);
        builder.Property(a => a.NewResponse).HasMaxLength(1000);

        builder.HasOne(a => a.Review)
            .WithMany()
            .HasForeignKey(a => a.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
