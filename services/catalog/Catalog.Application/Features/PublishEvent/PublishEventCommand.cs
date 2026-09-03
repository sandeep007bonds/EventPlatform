namespace Catalog.Application.Features.PublishEvent;

/// <summary>Command to publish a draft event, making it sellable.</summary>
/// <param name="Id">The event id to publish.</param>
/// <param name="TenantId">The caller's tenant id; must own the event.</param>
public sealed record PublishEventCommand(Guid Id, Guid TenantId) : IRequest<PublishEventResult>;
