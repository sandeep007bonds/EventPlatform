namespace Communication.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="DeliveryLogEntry"/> audit trail.</summary>
internal sealed class DeliveryLogEntryConfiguration : IEntityTypeConfiguration<DeliveryLogEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DeliveryLogEntry> builder)
    {
        builder.ToTable("delivery_log");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.Channel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Recipient).HasMaxLength(320);
        builder.Property(e => e.TemplateKey).HasMaxLength(100);
        builder.Property(e => e.Provider).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ProviderReference).HasMaxLength(200);
        builder.Property(e => e.FailureReason).HasMaxLength(500);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.CorrelationId);
    }
}
