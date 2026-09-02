namespace Venues.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="SeatMap"/> aggregate root.</summary>
internal sealed class SeatMapConfiguration : IEntityTypeConfiguration<SeatMap>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SeatMap> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("seat_maps");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.VenueId).IsRequired();
        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.PublishedVersionNumber);

        builder.HasIndex(m => m.VenueId);
        builder.HasIndex(m => new { m.TenantId, m.VenueId });

        builder.HasMany(m => m.Versions)
            .WithOne()
            .HasForeignKey(v => v.SeatMapId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(SeatMap.Versions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
