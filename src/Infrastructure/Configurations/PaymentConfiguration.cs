using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.OrderId);

        builder.Property(p => p.Method)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // AmountReceived is redundant with Status; nullable complex properties are not supported.
        builder.Ignore(p => p.AmountReceived);

        builder.ComplexProperty(p => p.AmountDue, cp =>
        {
            cp.Property(m => m.Amount)
                .HasColumnName("amount_due_amount")
                .HasPrecision(18, 2);
            cp.Property(m => m.Currency)
                .HasMaxLength(3)
                .HasColumnName("amount_due_currency");
        });
    }
}
