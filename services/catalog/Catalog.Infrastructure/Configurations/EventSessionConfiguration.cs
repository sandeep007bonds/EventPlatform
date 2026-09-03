namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="EventSession"/> entity.</summary>
internal sealed class EventSessionConfiguration : IEntityTypeConfiguration<EventSession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("event_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.EventId).IsRequired();
        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(100);
        builder.Property(s => s.StartsAt).IsRequired();
        builder.Property(s => s.EndsAt).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.SalesPaused).IsRequired();

        // The Venue service's ids, stored as plain columns. No foreign key: it is another service's
        // database, and a cross-service FK is the thing database-per-service exists to prevent.
        builder.Property(s => s.VenueId);
        builder.Property(s => s.SeatMapId);
        builder.Property(s => s.SeatMapVersionId);
        builder.Property(s => s.SeatMapVersionNumber);

        builder.OwnsOne(s => s.Venue, venue =>
        {
            venue.Property(v => v.Name).HasColumnName("venue_name").HasMaxLength(200);
            venue.Property(v => v.City).HasColumnName("venue_city").HasMaxLength(100);
            venue.Property(v => v.Country).HasColumnName("venue_country").HasMaxLength(2);
            venue.Property(v => v.TimeZoneId).HasColumnName("venue_time_zone_id").HasMaxLength(100);
        });

        builder.HasIndex(s => new { s.EventId, s.StartsAt });
        builder.HasIndex(s => new { s.TenantId, s.StartsAt });

        builder.HasMany(s => s.Allocations)
            .WithOne()
            .HasForeignKey(a => a.EventSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(EventSession.Allocations))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Computed from the allocations and the seat map in memory; no column behind it.
        builder.Ignore(s => s.IsSellable);
    }
}
