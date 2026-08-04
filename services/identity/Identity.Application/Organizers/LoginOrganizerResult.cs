namespace Identity.Application.Organizers;

/// <summary>The result of a <see cref="LoginOrganizerCommand"/>.</summary>
/// <param name="Outcome">The result.</param>
/// <param name="Token">The issued access token, populated only on <see cref="LoginOrganizerOutcome.LoggedIn"/>.</param>
/// <param name="OrganizerId">The organizer's stable id, populated only on <see cref="LoginOrganizerOutcome.LoggedIn"/>.</param>
/// <param name="TenantId">The organizer's tenant id, populated only on <see cref="LoginOrganizerOutcome.LoggedIn"/>.</param>
public sealed record LoginOrganizerResult(
    LoginOrganizerOutcome Outcome,
    IssuedAccessToken? Token,
    Guid? OrganizerId,
    Guid? TenantId);
