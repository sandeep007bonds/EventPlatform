namespace Venues.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="SeatMapElement"/> entity.</summary>
internal sealed class SeatMapElementConfiguration : IEntityTypeConfiguration<SeatMapElement>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SeatMapElement> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("seat_map_elements");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SeatMapVersionId).IsRequired();
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.Shape).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(e => e.X).IsRequired();
        builder.Property(e => e.Y).IsRequired();
        builder.Property(e => e.Width).IsRequired();
        builder.Property(e => e.Height).IsRequired();
        builder.Property(e => e.Rotation).IsRequired();
        builder.Property(e => e.Label).HasMaxLength(200);

        // jsonb, not text: it lets a future query reach into the geometry, and Postgres validates
        // the document on write so a malformed points array fails at the boundary rather than in
        // whichever client tries to draw it.
        builder.Property(e => e.PointsJson).HasColumnType("jsonb");
        builder.Property(e => e.StyleJson).HasColumnType("jsonb");

        builder.Property(e => e.VenueSectionId);
        builder.Property(e => e.AdmissionAreaId);

        builder.HasIndex(e => e.SeatMapVersionId);
    }
}
