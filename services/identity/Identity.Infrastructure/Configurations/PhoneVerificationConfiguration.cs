namespace Identity.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="PhoneVerification"/>.</summary>
internal sealed class PhoneVerificationConfiguration : IEntityTypeConfiguration<PhoneVerification>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PhoneVerification> builder)
    {
        builder.ToTable("phone_verifications");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(v => v.OtpHash).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Salt).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Non-unique, many historical challenges accumulate per phone number — ordered by
        // CreatedAt so "the latest challenge" (GetLatestPhoneVerificationAsync) is an index scan.
        builder.HasIndex(v => new { v.PhoneNumber, v.CreatedAt });
    }
}
