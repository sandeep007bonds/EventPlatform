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
        builder.Property(a => a.TicketTypeId).IsRequired();
        builder.Property(a => a.CatalogEventId).IsRequired();
        builder.Property(a => a.Version).IsConcurrencyToken();

        // One area can be sold under more than one ticket type, and each of those is its own pool
        // to count — so the type is part of the key, not just the area (ADR-0039).
        builder.HasIndex(a => new { a.EventSessionId, a.AdmissionAreaId, a.TicketTypeId }).IsUnique();
    }
}
