namespace Identity.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="OrganizerAccount"/>.</summary>
internal sealed class OrganizerAccountConfiguration : IEntityTypeConfiguration<OrganizerAccount>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OrganizerAccount> builder)
    {
        builder.ToTable("organizer_accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(a => a.Email).IsUnique();

        builder.Property(a => a.PasswordHash).IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
