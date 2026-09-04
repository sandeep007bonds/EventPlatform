namespace Inventory.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="SessionInventorySettings"/>.</summary>
internal sealed class SessionInventorySettingsConfiguration : IEntityTypeConfiguration<SessionInventorySettings>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SessionInventorySettings> builder)
    {
        builder.ToTable("session_inventory_settings");

        builder.HasKey(s => s.EventSessionId);

        builder.Property(s => s.CatalogEventId).IsRequired();
        builder.HasIndex(s => s.CatalogEventId);

        builder.Property(s => s.TenantId).IsRequired();
    }
}
