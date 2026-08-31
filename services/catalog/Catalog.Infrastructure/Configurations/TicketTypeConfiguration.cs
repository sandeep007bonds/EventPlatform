namespace Catalog.Infrastructure.Configurations;

/// <summary>EF Core mapping for the <see cref="TicketType"/> aggregate.</summary>
internal sealed class TicketTypeConfiguration : IEntityTypeConfiguration<TicketType>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TicketType> builder)
    {
        builder.ToTable("ticket_types");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.EventId).IsRequired();
        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.PriceMinor).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);

        // One name per event. This index is exact-match: a ticket-type name is displayed to buyers
        // exactly as the organizer typed it, so unlike PromoCode — which upper-cases its code on the
        // way in and gets case-insensitive uniqueness from a plain index for free — the stored value
        // keeps its casing and the index cannot be the whole story.
        //
        // Case-insensitive uniqueness is enforced in CreateTicketTypeHandler/UpdateTicketTypeHandler,
        // which compare against existing names with OrdinalIgnoreCase before writing. This index is
        // the backstop for an exact duplicate. Two concurrent requests creating "Gold" and "gold"
        // could still both land; that is an organizer-facing admin endpoint, the window is a few
        // milliseconds, and the fix is a functional unique index over lower(name) if it ever bites.
        builder.HasIndex(t => new { t.EventId, t.Name }).IsUnique();

        builder.HasIndex(t => t.EventId);
    }
}
