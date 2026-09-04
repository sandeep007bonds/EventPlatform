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

        builder.Property(t => t.TicketTypeId).IsRequired();

        builder.HasIndex(t => t.PromoCodeId);
    }
}
