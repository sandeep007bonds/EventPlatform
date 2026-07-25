namespace Ordering.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="OrderLine"/>.</summary>
internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("order_line");

        builder.HasKey(line => line.Id);

        builder.Property(line => line.PriceMinor).IsRequired();

        builder.HasIndex(line => line.OrderId);
    }
}
