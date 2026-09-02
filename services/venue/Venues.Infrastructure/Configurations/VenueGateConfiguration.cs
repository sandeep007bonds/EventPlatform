namespace Venues.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="VenueGate"/> entity.</summary>
internal sealed class VenueGateConfiguration : IEntityTypeConfiguration<VenueGate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<VenueGate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("venue_gates");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.VenueId).IsRequired();
        builder.Property(g => g.Code).HasMaxLength(32).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.Property(g => g.IsActive).IsRequired();

        // The aggregate enforces this too, over the gates it has loaded. The index is what makes it
        // true under two concurrent adds, which the aggregate cannot see.
        builder.HasIndex(g => new { g.VenueId, g.Code }).IsUnique();
    }
}
