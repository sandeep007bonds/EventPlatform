namespace Inventory.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="HoldItem"/>.</summary>
internal sealed class HoldItemConfiguration : IEntityTypeConfiguration<HoldItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HoldItem> builder)
    {
        builder.ToTable("hold_item");

        builder.HasKey(item => new { item.HoldId, item.InventoryItemId });
    }
}
