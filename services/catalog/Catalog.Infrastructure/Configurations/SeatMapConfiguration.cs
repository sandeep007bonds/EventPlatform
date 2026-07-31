namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="SeatMap"/> aggregate.</summary>
internal sealed class SeatMapConfiguration : IEntityTypeConfiguration<SeatMap>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SeatMap> builder)
    {
        builder.ToTable("seat_maps");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.EventId).IsRequired();

        // One seat map per event.
        builder.HasIndex(m => m.EventId).IsUnique();
        builder.HasIndex(m => new { m.TenantId, m.EventId });

        builder.HasMany(m => m.Seats)
            .WithOne()
            .HasForeignKey(s => s.SeatMapId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(SeatMap.Seats))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(m => m.GeneralAdmissionSections)
            .WithOne()
            .HasForeignKey(s => s.SeatMapId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(SeatMap.GeneralAdmissionSections))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
