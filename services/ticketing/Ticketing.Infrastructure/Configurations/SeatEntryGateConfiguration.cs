namespace Ticketing.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="SeatEntryGate"/> entity.</summary>
internal sealed class SeatEntryGateConfiguration : IEntityTypeConfiguration<SeatEntryGate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SeatEntryGate> builder)
    {
        builder.ToTable("seat_entry_gate");

        builder.HasKey(g => g.SeatId);

        builder.Property(g => g.EventId).IsRequired();
        builder.Property(g => g.EntryGateId).IsRequired();

        builder.HasIndex(g => g.EventId);
    }
}
