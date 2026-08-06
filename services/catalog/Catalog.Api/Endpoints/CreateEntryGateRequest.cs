namespace Catalog.Api.Endpoints;

/// <summary>
/// Request body for defining a new entry gate for an event. The tenant is taken from the
/// caller's token, never from this body (ADR-0011).
/// </summary>
/// <param name="Name">Gate name.</param>
public sealed record CreateEntryGateRequest(string Name);
