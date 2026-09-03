namespace Catalog.Domain;

/// <summary>
/// The handful of venue facts a storefront needs, copied onto an <see cref="EventSession"/> when a
/// venue is attached to it.
/// </summary>
/// <remarks>
/// <b>A cache, not the truth.</b> The Venue service owns this data; this is a copy so that listing
/// twenty events does not become twenty cross-service calls to render "Wankhede Stadium, Mumbai".
/// It is refreshed when the session's venue or seat map is re-attached, and it is deliberately
/// limited to fields whose staleness is cosmetic — a venue that has been renamed shows its old name
/// on an event page until someone touches the session, which is a far better failure than the event
/// list not loading.
/// <para>
/// Nothing is ever <i>decided</i> from this. Seat identity, gates and capacity are always read live
/// from Venue by id.
/// </para>
/// </remarks>
/// <param name="Name">Venue name, e.g. <c>DY Patil Stadium</c>.</param>
/// <param name="City">City the venue is in.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code.</param>
/// <param name="TimeZoneId">
/// The venue's IANA time zone (e.g. <c>Asia/Kolkata</c>), if it has one. Every stored instant is
/// already unambiguous; this exists so a client can render a 7pm Delhi show as 7pm rather than as
/// 1:30pm to a reader in London.
/// </param>
public sealed record VenueSnapshot(string Name, string City, string Country, string? TimeZoneId);
