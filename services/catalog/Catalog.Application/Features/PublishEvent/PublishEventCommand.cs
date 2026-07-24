namespace Catalog.Application.Features.PublishEvent;

/// <summary>Command to publish a draft event, making it sellable.</summary>
/// <param name="Id">The event id to publish.</param>
public sealed record PublishEventCommand(Guid Id) : IRequest<bool>;
