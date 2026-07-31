namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="EventGroup"/> aggregate.</summary>
internal sealed class EventGroupConfiguration : IEntityTypeConfiguration<EventGroup>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventGroup> builder)
    {
        builder.ToTable("event_groups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.TenantId).IsRequired();
        builder.Property(g => g.Title).HasMaxLength(200).IsRequired();

        builder.HasIndex(g => new { g.TenantId, g.Id });
    }
}
