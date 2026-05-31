using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure;

internal sealed class IdempotencyConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("idempotency_records");

        // Key is the client-supplied idempotency GUID — never DB-generated.
        builder.HasKey(r => r.Key);
        builder.Property(r => r.Key).ValueGeneratedNever();

        builder.Property(r => r.ResultJson).IsRequired();

        builder.Property(r => r.HttpStatusCode);

        builder.Property(r => r.CreatedAt);
    }
}
