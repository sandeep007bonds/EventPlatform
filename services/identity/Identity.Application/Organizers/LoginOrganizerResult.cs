namespace Identity.Application.Organizers;

/// <summary>The result of a <see cref="LoginOrganizerCommand"/>.</summary>
/// <param name="Outcome">The result.</param>
/// <param name="Token">Populated only for <see cref="LoginOrganizerOutcome.LoggedIn"/>.</param>
/// <param name="OrganizerId">Populated only for <see cref="LoginOrganizerOutcome.LoggedIn"/>.</param>
/// <param name="TenantId">Populated only for <see cref="LoginOrganizerOutcome.LoggedIn"/>.</param>
public sealed record LoginOrganizerResult(
    LoginOrganizerOutcome Outcome,
    IssuedAccessToken? Token,
    Guid? OrganizerId,
    Guid? TenantId);
