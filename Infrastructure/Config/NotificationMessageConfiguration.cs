using Core.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.Property(m => m.TemplateKey).HasMaxLength(200).IsRequired();
        builder.Property(m => m.RecipientEmail).HasMaxLength(256).IsRequired();
        builder.Property(m => m.Subject).HasMaxLength(200).IsRequired();
        builder.Property(m => m.HtmlBody).HasMaxLength(10000).IsRequired();
        builder.Property(m => m.TextBody).HasMaxLength(4000);
        builder.Property(m => m.Error).HasMaxLength(2000);
        builder.Property(m => m.Metadata).HasMaxLength(4000);
        builder.HasIndex(m => m.TemplateKey);
        builder.HasIndex(m => m.RecipientEmail);
        builder.HasIndex(m => m.Status);
    }
}
