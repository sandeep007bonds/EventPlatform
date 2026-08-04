namespace Identity.Application.Abstractions;

/// <summary>
/// Persistence abstraction for tenants and organizer accounts. One repository, shared
/// <see cref="SaveChangesAsync"/>, so a registration (new tenant + new account) lands in a single
/// transaction — same shape as <see cref="IIdentityRepository"/>.
/// </summary>
public interface IOrganizerRepository
{
    /// <summary>Registers a new tenant to be persisted.</summary>
    /// <param name="tenant">The tenant to add.</param>
    void AddTenant(Tenant tenant);

    /// <summary>Registers a new organizer account to be persisted.</summary>
    /// <param name="account">The account to add.</param>
    void AddOrganizerAccount(OrganizerAccount account);

    /// <summary>Returns the organizer account for an email, if one is registered.</summary>
    /// <param name="email">The login email.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<OrganizerAccount?> GetOrganizerByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
