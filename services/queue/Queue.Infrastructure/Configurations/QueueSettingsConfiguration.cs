namespace Queue.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="QueueSettings"/> entity.</summary>
internal sealed class QueueSettingsConfiguration : IEntityTypeConfiguration<QueueSettings>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<QueueSettings> builder)
    {
        builder.ToTable("queue_settings");

        builder.HasKey(s => s.EventId);

        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.Enabled).IsRequired();
        builder.Property(s => s.AdmissionRatePerInterval).IsRequired();
        builder.Property(s => s.IntervalSeconds).IsRequired();
        builder.Property(s => s.SessionTtlSeconds).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();

        builder.HasIndex(s => s.Enabled);
    }
}
