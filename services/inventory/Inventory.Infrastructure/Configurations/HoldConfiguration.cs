namespace Inventory.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="Hold"/> aggregate.</summary>
internal sealed class HoldConfiguration : IEntityTypeConfiguration<Hold>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Hold> builder)
    {
        builder.ToTable("hold");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.TenantId).IsRequired();
        builder.Property(h => h.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(h => h.CatalogEventId).IsRequired();

        builder.HasIndex(h => new { h.EventSessionId, h.Status });

        // The per-buyer limit is counted across the whole event, so that query filters on the
        // denormalised event id and needs its own index.
        builder.HasIndex(h => new { h.CatalogEventId, h.UserId, h.Status });
        builder.HasIndex(h => h.ExpiresAt);

        builder.HasMany(h => h.Items)
            .WithOne()
            .HasForeignKey(item => item.HoldId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Hold.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(h => h.GeneralAdmissionItems)
            .WithOne()
            .HasForeignKey(item => item.HoldId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Hold.GeneralAdmissionItems))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
