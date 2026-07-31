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
        builder.Property(e => e.AgeRestriction).HasMaxLength(50);
        builder.Property(e => e.BannerImageUrl).HasMaxLength(2000);
        builder.Property(e => e.VideoUrl).HasMaxLength(2000);

        builder.HasOne<Venue>().WithMany().HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.Id });
    }
}
