namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="Seat"/> entity.</summary>
internal sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("seats");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Section).HasMaxLength(100).IsRequired();
        builder.Property(s => s.PriceTier).HasMaxLength(50).IsRequired();
        builder.Property(s => s.PriceAmount).HasPrecision(18, 2);
        builder.Property(s => s.Row).HasMaxLength(10).IsRequired();
        builder.Property(s => s.Number).IsRequired();

        // Label is derived from the other columns; never stored.
        builder.Ignore(s => s.Label);

        builder.HasIndex(s => new { s.SeatMapId, s.Section, s.Row, s.Number }).IsUnique();
    }
}
