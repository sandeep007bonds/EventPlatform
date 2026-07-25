namespace Payments.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="Payment"/> aggregate.</summary>
internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payment");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.Provider).HasMaxLength(50).IsRequired();
        builder.Property(p => p.ProviderReference).HasMaxLength(200);
        builder.Property(p => p.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.FailureReason).HasMaxLength(200);

        // Idempotent charge: one payment per (order, idempotency key).
        builder.HasIndex(p => new { p.OrderId, p.IdempotencyKey }).IsUnique();
        builder.HasIndex(p => p.OrderId);
    }
}
