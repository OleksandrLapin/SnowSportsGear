using Core.Entities.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasIndex(o => o.OrderDate);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.BuyerEmail);

        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(o => o.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Discount).HasColumnType("decimal(18,2)");
        builder.Property(o => o.TrackingNumber).HasMaxLength(200);
        builder.Property(o => o.TrackingUrl).HasMaxLength(1000);
        builder.Property(o => o.CancelledBy).HasMaxLength(200);
        builder.Property(o => o.CancelledReason).HasMaxLength(1000);
        builder.Property(o => o.DeliveryUpdateDetails).HasMaxLength(2000);

        builder.OwnsOne(o => o.PaymentSummary, ps =>
        {
            ps.WithOwner();
        });

        builder.OwnsOne(o => o.ShippingAddress, sa =>
        {
            sa.WithOwner();
        });
    }
}
