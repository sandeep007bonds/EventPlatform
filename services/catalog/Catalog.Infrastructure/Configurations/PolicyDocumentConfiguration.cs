namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="PolicyDocument"/> aggregate.</summary>
internal sealed class PolicyDocumentConfiguration : IEntityTypeConfiguration<PolicyDocument>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PolicyDocument> builder)
    {
        builder.ToTable("policy_documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.Kind).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.BodyHtml).IsRequired();
        builder.Property(d => d.Version).IsRequired();
        builder.Property(d => d.UpdatedAt).IsRequired();

        builder.HasOne<Event>().WithMany().HasForeignKey(d => d.EventId).OnDelete(DeleteBehavior.Cascade);

        // One document per (tenant, scope, kind). Postgres treats NULLs as distinct in a unique
        // index by default, which would let a tenant hold any number of "default terms" rows — so
        // the default scope gets its own filtered index with EventId spelled out as NULL.
        builder.HasIndex(d => new { d.TenantId, d.EventId, d.Kind })
            .IsUnique()
            .HasFilter("\"EventId\" IS NOT NULL");

        builder.HasIndex(d => new { d.TenantId, d.Kind })
            .IsUnique()
            .HasFilter("\"EventId\" IS NULL");
    }
}
