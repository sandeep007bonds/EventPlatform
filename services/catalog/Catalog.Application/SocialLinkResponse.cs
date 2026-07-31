namespace Catalog.Application;

/// <summary>One (platform, URL) pair in a read model's social-links list.</summary>
/// <param name="Platform">Free-text platform name.</param>
/// <param name="Url">The link URL.</param>
public sealed record SocialLinkResponse(string Platform, string Url);
