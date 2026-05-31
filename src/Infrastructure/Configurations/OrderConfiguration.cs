using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.PriorityLevel)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Computed / transient properties have no column.
        builder.Ignore(o => o.DomainEvents);
        builder.Ignore(o => o.IsReady);
        builder.Ignore(o => o.TotalPrice);

        // EF Core discovers the _items backing field by convention (<_propertyName>).
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired();
    }
}
