namespace Inventory.Infrastructure.Configurations;

/// <summary>EF Core mapping for the append-only <see cref="LedgerEntry"/>.</summary>
internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("inventory_ledger");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedOnAdd();

        builder.Property(l => l.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.ToStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(l => l.Cause).HasMaxLength(20).IsRequired();

        builder.HasIndex(l => l.InventoryItemId);
    }
}
