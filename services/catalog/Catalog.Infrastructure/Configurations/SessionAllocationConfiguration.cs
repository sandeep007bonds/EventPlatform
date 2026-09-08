namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="SessionAllocation"/> entity.</summary>
internal sealed class SessionAllocationConfiguration : IEntityTypeConfiguration<SessionAllocation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SessionAllocation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("session_allocations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EventSessionId).IsRequired();

        // Same 32-char cap as the Venue seat map's section and admission-area codes — these values
        // are compared verbatim across the two services, so the widths have to agree.
        builder.Property(a => a.Code).HasMaxLength(32).IsRequired();

        // Nullable, because an excluded block is sold as nothing and so names no type. The aggregate
        // is what keeps that honest — priced or excluded, never both and never neither.
        builder.Property(a => a.TicketTypeId);

        builder.Property(a => a.IsExcluded).IsRequired();
        builder.Property(a => a.DisplayName).HasMaxLength(100);
        builder.Property(a => a.CapacityOverride);

        // The aggregate enforces this too, over the allocations it has loaded. The index is what
        // makes it true under two concurrent writes, which the aggregate cannot see.
        builder.HasIndex(a => new { a.EventSessionId, a.Code }).IsUnique();

        builder.HasOne<TicketType>().WithMany().HasForeignKey(a => a.TicketTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
