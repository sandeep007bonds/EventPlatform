namespace Catalog.Application.Features.PublishEvent;

/// <summary>The result of attempting to publish an event.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Problems">
/// Every reason a performance could not be sold, one per performance. A list rather than the first
/// failure because an organizer fixing a three-night run needs to see all three.
/// </param>
public sealed record PublishEventResult(PublishEventOutcome Outcome, IReadOnlyList<string> Problems);
