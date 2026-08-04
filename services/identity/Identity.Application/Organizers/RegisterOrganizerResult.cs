namespace Identity.Application.Organizers;

/// <summary>The result of a <see cref="RegisterOrganizerCommand"/>.</summary>
/// <param name="Outcome">The result.</param>
/// <param name="Token">Populated only for <see cref="RegisterOrganizerOutcome.Registered"/>.</param>
/// <param name="OrganizerId">Populated only for <see cref="RegisterOrganizerOutcome.Registered"/>.</param>
/// <param name="TenantId">Populated only for <see cref="RegisterOrganizerOutcome.Registered"/>.</param>
public sealed record RegisterOrganizerResult(
    RegisterOrganizerOutcome Outcome,
    IssuedAccessToken? Token,
    Guid? OrganizerId,
    Guid? TenantId);
