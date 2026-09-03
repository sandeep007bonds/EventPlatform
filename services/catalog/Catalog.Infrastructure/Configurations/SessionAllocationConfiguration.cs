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
        builder.Property(a => a.TicketTypeId).IsRequired();

        // The aggregate enforces this too, over the allocations it has loaded. The index is what
        // makes it true under two concurrent writes, which the aggregate cannot see.
        builder.HasIndex(a => new { a.EventSessionId, a.Code }).IsUnique();

        builder.HasOne<TicketType>().WithMany().HasForeignKey(a => a.TicketTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
