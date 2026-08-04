namespace Identity.Api.Endpoints;

/// <summary>Response body for a successful organizer registration or login.</summary>
/// <param name="AccessToken">The issued JWT.</param>
/// <param name="TokenType">Always <c>"Bearer"</c>.</param>
/// <param name="ExpiresAt">When the token stops being valid (UTC).</param>
/// <param name="OrganizerId">The organizer's stable id (matches the token's <c>sub</c> claim).</param>
/// <param name="TenantId">The organizer's tenant id (matches the token's <c>tenant_id</c> claim).</param>
public sealed record OrganizerAuthResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, Guid OrganizerId, Guid TenantId);
