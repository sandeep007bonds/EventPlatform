namespace Ordering.Infrastructure;

/// <summary>The Inventory extend-hold response deserialized by the hold client.</summary>
/// <param name="ExpiresAt">The hold's new expiry (UTC).</param>
internal sealed record ExtendHoldResponse(DateTimeOffset ExpiresAt);
