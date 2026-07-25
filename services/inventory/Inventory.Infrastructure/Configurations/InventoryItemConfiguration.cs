namespace Inventory.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="InventoryItem"/>.</summary>
internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_item");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.TenantId).IsRequired();
        builder.Property(i => i.PriceTier).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.Version).IsConcurrencyToken();

        builder.HasIndex(i => new { i.EventId, i.SeatId }).IsUnique();
        builder.HasIndex(i => new { i.EventId, i.Status });
    }
}
