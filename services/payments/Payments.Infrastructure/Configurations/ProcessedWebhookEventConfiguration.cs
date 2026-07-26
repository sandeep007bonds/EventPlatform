namespace Payments.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="ProcessedWebhookEvent"/> idempotency ledger.</summary>
internal sealed class ProcessedWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedWebhookEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ProcessedWebhookEvent> builder)
    {
        builder.ToTable("processed_webhook_event");

        // The provider event id is the natural key — its uniqueness is what enforces dedupe.
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId).HasMaxLength(200);
        builder.Property(e => e.EventType).HasMaxLength(100).IsRequired();
    }
}
