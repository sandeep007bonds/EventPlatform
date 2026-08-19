namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="PromoCodeTier"/> entity.</summary>
internal sealed class PromoCodeTierConfiguration : IEntityTypeConfiguration<PromoCodeTier>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PromoCodeTier> builder)
    {
        builder.ToTable("promo_code_tiers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.PromoCodeId).IsRequired();

        // Same 50-char cap as Seat.PriceTier / GeneralAdmissionSection.PriceTier — these values are
        // compared against those verbatim, so a shorter cap here would silently truncate a legal tier.
        builder.Property(t => t.PriceTier).HasMaxLength(50).IsRequired();

        builder.HasIndex(t => t.PromoCodeId);
    }
}
