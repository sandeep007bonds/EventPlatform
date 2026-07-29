namespace Communication.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="ProcessedNotificationEvent"/> idempotency ledger.</summary>
internal sealed class ProcessedNotificationEventConfiguration : IEntityTypeConfiguration<ProcessedNotificationEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProcessedNotificationEvent> builder)
    {
        builder.ToTable("processed_notification_event");

        // The integration event's own id is the natural key — its uniqueness is what enforces dedupe.
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
    }
}
