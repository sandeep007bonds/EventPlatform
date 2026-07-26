namespace Ticketing.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="Ticket"/> aggregate.</summary>
internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("ticket");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.Token).HasMaxLength(64).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(t => t.OrderId);
        builder.HasIndex(t => new { t.OrderId, t.SeatId }).IsUnique();
        builder.HasIndex(t => t.Token).IsUnique();
    }
}
