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
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.TenantId).IsRequired();

        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.Category).HasMaxLength(100);
        builder.Property(e => e.TimeZoneId).HasMaxLength(100);
        builder.Property(e => e.AgeRestriction).HasMaxLength(50);
        builder.Property(e => e.BannerImageUrl).HasMaxLength(2000);
        builder.Property(e => e.VideoUrl).HasMaxLength(2000);

        builder.Property(e => e.LocationName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.AddressLine1).HasMaxLength(200).IsRequired();
        builder.Property(e => e.AddressLine2).HasMaxLength(200);
        builder.Property(e => e.City).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Region).HasMaxLength(100);
        builder.Property(e => e.PostalCode).HasMaxLength(20);
        builder.Property(e => e.Country).HasMaxLength(2).IsRequired();

        builder.Property(e => e.ContactPhone).HasMaxLength(30);
        builder.Property(e => e.ContactMobile).HasMaxLength(30);
        builder.Property(e => e.ContactEmail).HasMaxLength(200);
        builder.Property(e => e.WebsiteUrl).HasMaxLength(2000);

        builder.HasOne<EventGroup>().WithMany().HasForeignKey(e => e.EventGroupId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.SocialLinks)
            .WithOne()
            .HasForeignKey(l => l.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Event.SocialLinks))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(e => new { e.TenantId, e.Id });
        builder.HasIndex(e => e.EventGroupId);
    }
}
