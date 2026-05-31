using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure;

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.MenuItemId);

        builder.Property(i => i.SnackType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.PriorityLevel)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(i => i.EnqueuedAt);
        builder.Property(i => i.StartedBakingAt);
        builder.Property(i => i.ReadyAt);

        // Money is stored inline (two columns) using EF Core 8 complex type.
        builder.ComplexProperty(i => i.UnitPrice, cp =>
        {
            cp.Property(m => m.Amount)
                .HasColumnName("unit_price_amount")
                .HasPrecision(18, 2);
            cp.Property(m => m.Currency)
                .HasMaxLength(3)
                .HasColumnName("unit_price_currency");
        });
    }
}
