namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="Venue"/> aggregate.</summary>
internal sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable("venues");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.TenantId).IsRequired();
        builder.Property(v => v.Name).HasMaxLength(200).IsRequired();
        builder.Property(v => v.AddressLine1).HasMaxLength(200).IsRequired();
        builder.Property(v => v.AddressLine2).HasMaxLength(200);
        builder.Property(v => v.City).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Region).HasMaxLength(100);
        builder.Property(v => v.PostalCode).HasMaxLength(20);
        builder.Property(v => v.Country).HasMaxLength(2).IsRequired();

        builder.HasIndex(v => new { v.TenantId, v.Id });
    }
}
