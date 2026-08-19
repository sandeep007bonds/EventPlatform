namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="PromoCode"/> aggregate.</summary>
internal sealed class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.ToTable("promo_codes");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.EventId).IsRequired();
        builder.Property(p => p.TenantId).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500);
        builder.Property(p => p.DiscountType).HasConversion<string>().HasMaxLength(20).IsRequired();

        // 18,4 rather than the money-ish 18,2: this column holds a *percentage* as often as an
        // amount, and a rate like 7.5% deserves more than two decimals of headroom.
        builder.Property(p => p.DiscountValue).HasPrecision(18, 4).IsRequired();

        builder.HasMany(p => p.Tiers)
            .WithOne()
            .HasForeignKey(t => t.PromoCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(PromoCode.Tiers))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // One code string per event. Codes are stored upper-invariant (PromoCode.Create), so a
        // plain unique index gives case-insensitive uniqueness without a citext column or a
        // case-insensitive collation.
        builder.HasIndex(p => new { p.EventId, p.Code }).IsUnique();
    }
}
