namespace Identity.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="BuyerAccount"/>.</summary>
internal sealed class BuyerAccountConfiguration : IEntityTypeConfiguration<BuyerAccount>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BuyerAccount> builder)
    {
        builder.ToTable("buyer_accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(a => a.PhoneNumber).IsUnique();
    }
}
