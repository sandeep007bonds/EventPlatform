namespace Catalog.Api.Endpoints;

/// <summary>Request body for changing a draft event's public slug.</summary>
/// <param name="Slug">The requested slug; normalized server-side, so "My Show!" is accepted.</param>
public sealed record ChangeEventSlugRequest(string Slug);
