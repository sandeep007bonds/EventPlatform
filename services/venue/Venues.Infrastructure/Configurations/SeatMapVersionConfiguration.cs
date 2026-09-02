namespace Venues.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="SeatMapVersion"/> entity.</summary>
internal sealed class SeatMapVersionConfiguration : IEntityTypeConfiguration<SeatMapVersion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SeatMapVersion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("seat_map_versions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.SeatMapId).IsRequired();
        builder.Property(v => v.VersionNumber).IsRequired();
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.PublishedAt);

        builder.HasIndex(v => new { v.SeatMapId, v.VersionNumber }).IsUnique();

        builder.HasMany(v => v.Sections)
            .WithOne()
            .HasForeignKey(s => s.SeatMapVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(SeatMapVersion.Sections))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(v => v.AdmissionAreas)
            .WithOne()
            .HasForeignKey(a => a.SeatMapVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(SeatMapVersion.AdmissionAreas))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(v => v.Elements)
            .WithOne()
            .HasForeignKey(e => e.SeatMapVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(SeatMapVersion.Elements))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
