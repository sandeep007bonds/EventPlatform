namespace Venues.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="AdmissionArea"/> entity.</summary>
internal sealed class AdmissionAreaConfiguration : IEntityTypeConfiguration<AdmissionArea>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AdmissionArea> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("admission_areas");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.SeatMapVersionId).IsRequired();
        builder.Property(a => a.Code).HasMaxLength(32).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Capacity).IsRequired();
        builder.Property(a => a.DisplayOrder).IsRequired();
        builder.Property(a => a.GateId);
        builder.Property(a => a.TierLabel).HasMaxLength(100);

        builder.HasIndex(a => new { a.SeatMapVersionId, a.Code }).IsUnique();
    }
}
