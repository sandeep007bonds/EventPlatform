namespace Ordering.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="Order"/> aggregate.</summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.TenantId).IsRequired();
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(o => o.FailureReason).HasMaxLength(200);

        // Idempotent checkout: one order per (tenant, idempotency key).
        builder.HasIndex(o => new { o.TenantId, o.IdempotencyKey }).IsUnique();
        builder.HasIndex(o => new { o.TenantId, o.UserId });

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(line => line.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Order.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
