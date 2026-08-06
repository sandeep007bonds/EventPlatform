namespace Ticketing.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="EventScanContext"/> entity.</summary>
internal sealed class EventScanContextConfiguration : IEntityTypeConfiguration<EventScanContext>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventScanContext> builder)
    {
        builder.ToTable("event_scan_context");

        builder.HasKey(c => c.EventId);

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.StartsAt).IsRequired();
        builder.Property(c => c.EndsAt).IsRequired();
    }
}
