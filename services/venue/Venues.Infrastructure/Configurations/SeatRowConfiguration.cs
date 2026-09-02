namespace Venues.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="SeatRow"/> entity.</summary>
internal sealed class SeatRowConfiguration : IEntityTypeConfiguration<SeatRow>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SeatRow> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("seat_rows");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.VenueSectionId).IsRequired();
        builder.Property(r => r.Label).HasMaxLength(16).IsRequired();
        builder.Property(r => r.DisplayOrder).IsRequired();

        builder.HasIndex(r => new { r.VenueSectionId, r.Label }).IsUnique();

        builder.HasMany(r => r.Seats)
            .WithOne()
            .HasForeignKey(s => s.SeatRowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(SeatRow.Seats))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
