namespace Inventory.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="HoldGeneralAdmissionItem"/>.</summary>
internal sealed class HoldGeneralAdmissionItemConfiguration : IEntityTypeConfiguration<HoldGeneralAdmissionItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HoldGeneralAdmissionItem> builder)
    {
        builder.ToTable("hold_general_admission_item");

        builder.HasKey(item => new { item.HoldId, item.GeneralAdmissionAllocationId });
    }
}
