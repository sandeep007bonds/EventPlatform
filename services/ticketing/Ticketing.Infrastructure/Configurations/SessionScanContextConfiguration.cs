namespace Ticketing.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="SessionScanContext"/> entity.</summary>
internal sealed class SessionScanContextConfiguration : IEntityTypeConfiguration<SessionScanContext>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SessionScanContext> builder)
    {
        builder.ToTable("event_scan_context");

        builder.HasKey(c => c.EventSessionId);

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.StartsAt).IsRequired();
        builder.Property(c => c.EndsAt).IsRequired();
    }
}
