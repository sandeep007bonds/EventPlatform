namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="EntryGate"/> entity.</summary>
internal sealed class EntryGateConfiguration : IEntityTypeConfiguration<EntryGate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EntryGate> builder)
    {
        builder.ToTable("entry_gates");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.TenantId).IsRequired();
        builder.Property(g => g.EventId).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(g => g.EventId);
    }
}
