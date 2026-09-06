namespace Venues.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="VenueSection"/> entity.</summary>
internal sealed class VenueSectionConfiguration : IEntityTypeConfiguration<VenueSection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<VenueSection> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("venue_sections");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SeatMapVersionId).IsRequired();
        builder.Property(s => s.Code).HasMaxLength(32).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.DisplayOrder).IsRequired();
        builder.Property(s => s.GateId);
        builder.Property(s => s.TierLabel).HasMaxLength(100);

        builder.HasIndex(s => new { s.SeatMapVersionId, s.Code }).IsUnique();

        builder.HasMany(s => s.Rows)
            .WithOne()
            .HasForeignKey(r => r.VenueSectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(VenueSection.Rows))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Computed from the rows in memory; there is no column behind them.
        builder.Ignore(s => s.SeatCount);
        builder.Ignore(s => s.SellableSeatCount);
    }
}
