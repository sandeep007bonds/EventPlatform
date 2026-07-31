namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="EventGroupSocialLink"/> entity.</summary>
internal sealed class EventGroupSocialLinkConfiguration : IEntityTypeConfiguration<EventGroupSocialLink>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventGroupSocialLink> builder)
    {
        builder.ToTable("event_group_social_links");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Platform).HasMaxLength(50).IsRequired();
        builder.Property(l => l.Url).HasMaxLength(2000).IsRequired();

        builder.HasIndex(l => l.EventGroupId);
    }
}
