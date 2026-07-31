namespace Inventory.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="GeneralAdmissionAllocation"/>.</summary>
internal sealed class GeneralAdmissionAllocationConfiguration : IEntityTypeConfiguration<GeneralAdmissionAllocation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GeneralAdmissionAllocation> builder)
    {
        builder.ToTable("general_admission_allocation");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.PriceTier).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Version).IsConcurrencyToken();

        builder.HasIndex(a => new { a.EventId, a.CatalogSectionId }).IsUnique();
    }
}
