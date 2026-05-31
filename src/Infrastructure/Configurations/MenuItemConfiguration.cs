using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure;

internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_items");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Name).HasMaxLength(200);

        builder.Property(m => m.SnackType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.IsRemoved);

        builder.ComplexProperty(m => m.Price, cp =>
        {
            cp.Property(p => p.Amount)
                .HasColumnName("price_amount")
                .HasPrecision(18, 2);
            cp.Property(p => p.Currency)
                .HasMaxLength(3)
                .HasColumnName("price_currency");
        });
    }
}
