namespace Identity.Application.Organizers;

/// <summary>The result of a <see cref="RegisterOrganizerCommand"/>.</summary>
/// <param name="Outcome">The result.</param>
/// <param name="Token">The issued access token, populated only on <see cref="RegisterOrganizerOutcome.Registered"/>.</param>
/// <param name="OrganizerId">The new organizer's stable id, populated only on <see cref="RegisterOrganizerOutcome.Registered"/>.</param>
/// <param name="TenantId">The new tenant's id, populated only on <see cref="RegisterOrganizerOutcome.Registered"/>.</param>
public sealed record RegisterOrganizerResult(
    RegisterOrganizerOutcome Outcome,
    IssuedAccessToken? Token,
    Guid? OrganizerId,
    Guid? TenantId);
