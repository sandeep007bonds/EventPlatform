using MediatR;

namespace Catalog.Application.Features.GetEvent;

/// <summary>Query to fetch a single event by id.</summary>
/// <param name="Id">The event id.</param>
public sealed record GetEventQuery(Guid Id) : IRequest<EventResponse?>;
