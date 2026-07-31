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

        builder.Property(g => g.ContactPhone).HasMaxLength(30);
        builder.Property(g => g.ContactMobile).HasMaxLength(30);
        builder.Property(g => g.ContactEmail).HasMaxLength(200);
        builder.Property(g => g.WebsiteUrl).HasMaxLength(2000);

        builder.HasMany(g => g.SocialLinks)
            .WithOne()
            .HasForeignKey(l => l.EventGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(EventGroup.SocialLinks))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(g => new { g.TenantId, g.Id });
    }
}
