namespace Venues.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="Venue"/> aggregate.</summary>
internal sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("venues");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.TenantId).IsRequired();
        builder.Property(v => v.Name).HasMaxLength(200).IsRequired();
        builder.Property(v => v.VenueType).HasMaxLength(100);
        builder.Property(v => v.TimeZoneId).HasMaxLength(100);
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Owned rather than a table of its own: an address has no identity or lifecycle apart from
        // the venue it locates, and a join to read one would buy nothing.
        builder.OwnsOne(v => v.Address, address =>
        {
            address.Property(a => a.AddressLine1).HasColumnName("address_line1").HasMaxLength(200).IsRequired();
            address.Property(a => a.AddressLine2).HasColumnName("address_line2").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("city").HasMaxLength(100).IsRequired();
            address.Property(a => a.Region).HasColumnName("region").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("country").HasMaxLength(2).IsRequired();
            address.Property(a => a.Latitude).HasColumnName("latitude");
            address.Property(a => a.Longitude).HasColumnName("longitude");
        });

        builder.HasIndex(v => new { v.TenantId, v.Status });
        builder.HasIndex(v => new { v.TenantId, v.Name });

        builder.HasMany(v => v.Gates)
            .WithOne()
            .HasForeignKey(g => g.VenueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Venue.Gates))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(v => v.Facilities)
            .WithOne()
            .HasForeignKey(f => f.VenueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Venue.Facilities))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
