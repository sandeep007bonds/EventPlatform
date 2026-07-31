namespace Inventory.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="EventInventorySettings"/>.</summary>
internal sealed class EventInventorySettingsConfiguration : IEntityTypeConfiguration<EventInventorySettings>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventInventorySettings> builder)
    {
        builder.ToTable("event_inventory_settings");

        builder.HasKey(s => s.EventId);

        builder.Property(s => s.TenantId).IsRequired();
    }
}
