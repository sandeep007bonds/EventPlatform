namespace Catalog.Application.Abstractions;

/// <summary>
/// Persistence abstraction for the <see cref="PromoCode"/> aggregate. Implemented in the
/// Infrastructure layer so the Application layer stays free of EF Core.
/// </summary>
public interface IPromoCodeRepository
{
    /// <summary>Registers a new promo code to be persisted.</summary>
    /// <param name="promoCode">The promo code to add.</param>
    void Add(PromoCode promoCode);

    /// <summary>Gets a promo code by id, tracked for update, or <see langword="null"/>.</summary>
    /// <param name="id">The promo-code id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The promo code, or <see langword="null"/>.</returns>
    Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Finds an event's promo code by the string a buyer typed. The lookup upper-cases
    /// <paramref name="code"/> first, matching how <see cref="PromoCode.Create"/> stores it, so
    /// matching is case-insensitive without a case-insensitive column collation.
    /// </summary>
    /// <param name="eventId">The event the code belongs to.</param>
    /// <param name="code">The code as typed, in any case.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The promo code, or <see langword="null"/> if the event has no such code.</returns>
    Task<PromoCode?> GetByCodeAsync(Guid eventId, string code, CancellationToken cancellationToken);

    /// <summary>Lists every promo code defined for an event, active or not — the organizer's view.</summary>
    /// <param name="eventId">The event id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The event's promo codes, newest first.</returns>
    Task<IReadOnlyList<PromoCode>> ListForEventAsync(Guid eventId, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when changes are saved.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
