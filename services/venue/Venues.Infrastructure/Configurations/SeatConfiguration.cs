namespace Venues.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="Seat"/> entity.</summary>
internal sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("seats");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SeatRowId).IsRequired();
        builder.Property(s => s.Number).HasMaxLength(16).IsRequired();
        builder.Property(s => s.IsSellable).IsRequired();

        // Stored as the integer flag set, not as text: it is a bitmask, and a comma-separated
        // string would make "which seats are accessible" a LIKE query.
        builder.Property(s => s.Attributes).HasConversion<int>().IsRequired();

        builder.HasIndex(s => new { s.SeatRowId, s.Number }).IsUnique();
    }
}
