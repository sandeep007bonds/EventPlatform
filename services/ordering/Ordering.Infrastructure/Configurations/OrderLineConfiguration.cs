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

        // The ticket type this line was sold as — an id, not the free-text tier name it replaced,
        // so a code scoped to a type still matches after the type is renamed.
        builder.Property(line => line.TicketTypeId).IsRequired();
        builder.Property(line => line.UnitPriceMinor).IsRequired();
        builder.Property(line => line.PriceMinor).IsRequired();

        builder.HasIndex(line => line.OrderId);
    }
}
