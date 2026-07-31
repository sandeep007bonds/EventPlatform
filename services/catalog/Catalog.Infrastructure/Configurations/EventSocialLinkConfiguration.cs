namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="EventSocialLink"/> entity.</summary>
internal sealed class EventSocialLinkConfiguration : IEntityTypeConfiguration<EventSocialLink>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EventSocialLink> builder)
    {
        builder.ToTable("event_social_links");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Platform).HasMaxLength(50).IsRequired();
        builder.Property(l => l.Url).HasMaxLength(2000).IsRequired();

        builder.HasIndex(l => l.EventId);
    }
}
