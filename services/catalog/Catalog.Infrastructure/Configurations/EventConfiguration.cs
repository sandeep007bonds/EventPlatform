namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="Event"/> aggregate.</summary>
internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Slug).HasMaxLength(EventSlug.MaxLength).IsRequired();
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();

        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.Category).HasMaxLength(100);
        builder.Property(e => e.AgeRestriction).HasMaxLength(50);
        builder.Property(e => e.BannerImageUrl).HasMaxLength(2000);
        builder.Property(e => e.VideoUrl).HasMaxLength(2000);

        builder.Property(e => e.ContactPhone).HasMaxLength(30);
        builder.Property(e => e.ContactMobile).HasMaxLength(30);
        builder.Property(e => e.ContactEmail).HasMaxLength(200);
        builder.Property(e => e.WebsiteUrl).HasMaxLength(2000);

        builder.HasOne<EventGroup>().WithMany().HasForeignKey(e => e.EventGroupId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Sessions)
            .WithOne()
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Event.Sessions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.SocialLinks)
            .WithOne()
            .HasForeignKey(l => l.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Event.SocialLinks))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Unique platform-wide, not per tenant: the slug is the whole of a public URL, and two
        // tenants cannot both own /events/coldplay-mumbai. This index is also the real guard
        // against the create-time race in CreateEventHandler, which checks and then writes.
        builder.HasIndex(e => e.Slug).IsUnique();

        builder.HasIndex(e => new { e.TenantId, e.Id });
        builder.HasIndex(e => e.EventGroupId);

        // The storefront lists events by date and filters to what is still upcoming. These two
        // columns are denormalised from the performances precisely so that stays one indexed scan
        // instead of loading every session of every event to find its earliest night.
        builder.HasIndex(e => new { e.Status, e.FirstSessionStartsAt });
    }
}
