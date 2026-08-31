namespace Catalog.Application.Abstractions;

/// <summary>
/// Turns a seat-map section's tier name into the <see cref="TicketType"/> it is sold as, creating
/// the type when the event has none by that name.
/// </summary>
/// <remarks>
/// <para>
/// This exists so the seat-map request shape did not have to change when ticket types were
/// introduced: sections still name a tier and a price, and the type is derived. Callers that want
/// to set a sales window or a per-buyer cap create the type explicitly first, through
/// <c>POST /v1/events/{id}/ticket-types</c>, and the section then simply finds it.
/// </para>
/// <para>
/// <b>An existing type's price wins.</b> Where a section names a tier that already exists at a
/// different price, the type's price is used and the section's is ignored — under this model the
/// type owns the price, and two prices under one name is a contradiction rather than a choice. The
/// contradiction is worth catching earlier where it is cheap: the seat-map validators reject a
/// single request that names the same tier at two different prices, which is the case an organizer
/// can actually see and fix.
/// </para>
/// </remarks>
/// <param name="ticketTypes">The ticket-type repository.</param>
internal sealed class TicketTypeResolver(ITicketTypeRepository ticketTypes)
{
    /// <summary>Minor units per major unit; the seat-map request still carries major-unit prices.</summary>
    private const decimal MinorUnitsPerMajor = 100m;

    /// <summary>Finds or creates the ticket type a section is sold as.</summary>
    /// <param name="eventId">The event the section belongs to.</param>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="tierName">The tier name from the section input.</param>
    /// <param name="priceAmount">The section's price in major units, used only when creating.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The existing or newly-created ticket type.</returns>
    public async Task<TicketType> ResolveAsync(
        Guid eventId,
        Guid tenantId,
        string tierName,
        decimal priceAmount,
        CancellationToken cancellationToken)
    {
        var existing = await ticketTypes.GetByNameAsync(eventId, tierName, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var created = TicketType.Create(
            eventId,
            tenantId,
            tierName,
            (long)Math.Round(priceAmount * MinorUnitsPerMajor, MidpointRounding.AwayFromZero));

        // Added but not saved: the caller's own SaveChangesAsync commits the type and the seat map
        // in one transaction, so a seat map can never reference a type that failed to persist.
        ticketTypes.Add(created);
        return created;
    }
}
