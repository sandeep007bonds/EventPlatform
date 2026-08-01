namespace Identity.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="SigningKey"/>.</summary>
internal sealed class SigningKeyConfiguration : IEntityTypeConfiguration<SigningKey>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SigningKey> builder)
    {
        builder.ToTable("signing_keys");

        builder.HasKey(k => k.Id);

        // Unbounded (Postgres text) — a base64'd PKCS8 blob has no natural length cap.
        builder.Property(k => k.PrivateKeyPkcs8Base64).IsRequired();

        // Partial unique index: a DB-level safety net so at most one row can ever be active,
        // on top of the application-level generate-once-under-a-lock logic.
        builder.HasIndex(k => k.IsActive)
            .IsUnique()
            .HasFilter("\"IsActive\" = true");
    }
}
