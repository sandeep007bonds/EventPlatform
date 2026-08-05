namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="GeneralAdmissionSection"/> entity.</summary>
internal sealed class GeneralAdmissionSectionConfiguration : IEntityTypeConfiguration<GeneralAdmissionSection>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GeneralAdmissionSection> builder)
    {
        builder.ToTable("seat_map_ga_sections");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SectionName).HasMaxLength(100).IsRequired();
        builder.Property(s => s.PriceTier).HasMaxLength(50).IsRequired();
        builder.Property(s => s.PriceAmount).HasPrecision(18, 2);
        builder.Property(s => s.Capacity).IsRequired();
        builder.Property(s => s.EntryGateId);

        builder.HasIndex(s => new { s.SeatMapId, s.SectionName }).IsUnique();
    }
}
