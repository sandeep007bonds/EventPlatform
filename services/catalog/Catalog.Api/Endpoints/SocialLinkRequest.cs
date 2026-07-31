namespace Catalog.Api.Endpoints;

/// <summary>One (platform, URL) pair in an open social-links list.</summary>
/// <param name="Platform">Free-text platform name (e.g. "Instagram", "X", "TikTok").</param>
/// <param name="Url">The link URL.</param>
public sealed record SocialLinkRequest(string Platform, string Url);
