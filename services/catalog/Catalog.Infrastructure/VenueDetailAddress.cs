namespace Catalog.Infrastructure;

/// <summary>The part of a venue's address Catalog caches for display.</summary>
/// <param name="City">City.</param>
/// <param name="Country">ISO 3166-1 alpha-2 country code.</param>
internal sealed record VenueDetailAddress(string City, string Country);
