namespace Venues.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="VenueFacility"/> entity.</summary>
internal sealed class VenueFacilityConfiguration : IEntityTypeConfiguration<VenueFacility>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<VenueFacility> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("venue_facilities");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.VenueId).IsRequired();
        builder.Property(f => f.Name).HasMaxLength(100).IsRequired();
        builder.Property(f => f.Description).HasMaxLength(500);

        builder.HasIndex(f => f.VenueId);
    }
}
