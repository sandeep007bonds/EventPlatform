namespace Catalog.Api.Endpoints;

/// <summary>
/// Request body for creating an event group. The tenant is taken from the caller's token, never
/// from this body (ADR-0011).
/// </summary>
/// <param name="Title">Group title (e.g. "Coldplay World Tour").</param>
public sealed record CreateEventGroupRequest(string Title);
