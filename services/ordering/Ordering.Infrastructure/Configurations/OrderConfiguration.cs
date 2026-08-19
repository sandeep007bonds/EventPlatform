namespace Ordering.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="Order"/> aggregate.</summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.TenantId).IsRequired();
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.FailureReason).HasMaxLength(200);
        builder.Property(o => o.BuyerEmail).HasMaxLength(320);
        builder.Property(o => o.PaymentClientSecret).HasMaxLength(500);
        builder.Property(o => o.TaxLabel).HasMaxLength(50);
        builder.Property(o => o.PromoCodeText).HasMaxLength(50);

        // Same precision as Catalog's own PromoCode.DiscountValue — a rate like 7.5% needs more
        // than the two decimals a money column would give.
        builder.Property(o => o.TaxRatePercent).HasPrecision(18, 4);

        // Idempotent checkout: one order per (buyer, idempotency key) — a checkout attempt is a
        // buyer action, not a tenant action (ADR-0022); UserId is always populated (JWT `sub`) even
        // for a buyer token with no tenant_id claim.
        builder.HasIndex(o => new { o.UserId, o.IdempotencyKey }).IsUnique();
        builder.HasIndex(o => new { o.TenantId, o.UserId });

        // Redemption caps are enforced by counting orders that carry a given code, so that count
        // must not be a sequential scan of every order ever placed. Most rows have a NULL here,
        // which a btree stores compactly and never has to search for the counting query.
        builder.HasIndex(o => o.PromoCodeId);

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(line => line.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Order.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
