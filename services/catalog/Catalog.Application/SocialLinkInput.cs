namespace Catalog.Application;

/// <summary>
/// One (platform, URL) pair in an open social-links list — shared shape for both
/// <see cref="Features.CreateEventGroup.CreateEventGroupCommand"/>/event-group updates and
/// <see cref="Features.UpdateEventDetails.UpdateEventDetailsCommand"/>.
/// </summary>
/// <param name="Platform">Free-text platform name (e.g. "Instagram", "X", "TikTok").</param>
/// <param name="Url">The link URL.</param>
public sealed record SocialLinkInput(string Platform, string Url);
