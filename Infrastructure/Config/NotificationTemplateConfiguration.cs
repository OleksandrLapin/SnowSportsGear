using Core.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.HasIndex(t => t.Key).IsUnique();
        builder.Property(t => t.Key).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Subject).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Headline).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Body).HasMaxLength(4000).IsRequired();
        builder.Property(t => t.CtaLabel).HasMaxLength(120);
        builder.Property(t => t.CtaUrl).HasMaxLength(1000);
        builder.Property(t => t.Footer).HasMaxLength(1000);
    }
}
