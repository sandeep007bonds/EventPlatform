namespace Ordering.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="OrderLine"/>.</summary>
internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("order_line");

        builder.HasKey(line => line.Id);

        builder.Property(line => line.Quantity).IsRequired();
        // Same 50-char cap the tier name has in Catalog and Inventory — this value is compared
        // against a promo code's tier list verbatim, so a shorter cap here would silently truncate.
        builder.Property(line => line.PriceTier).HasMaxLength(50).IsRequired();
        builder.Property(line => line.UnitPriceMinor).IsRequired();
        builder.Property(line => line.PriceMinor).IsRequired();

        builder.HasIndex(line => line.OrderId);
    }
}
