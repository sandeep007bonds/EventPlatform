namespace Ticketing.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="GaAllocationGate"/> entity.</summary>
internal sealed class GaAllocationGateConfiguration : IEntityTypeConfiguration<GaAllocationGate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GaAllocationGate> builder)
    {
        builder.ToTable("ga_allocation_gate");

        builder.HasKey(g => g.AllocationId);

        builder.Property(g => g.EventSessionId).IsRequired();
        builder.Property(g => g.EntryGateId).IsRequired();

        builder.HasIndex(g => g.EventSessionId);
    }
}
