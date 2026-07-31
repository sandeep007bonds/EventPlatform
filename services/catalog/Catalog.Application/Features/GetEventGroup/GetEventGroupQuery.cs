namespace Catalog.Application.Features.GetEventGroup;

/// <summary>
/// Query to fetch a single event group by id. Unlike <see cref="Catalog.Application.Features.GetEvent.GetEventQuery"/>,
/// there is no tenant-visibility rule — a group by itself, unlinked from any event, reveals
/// nothing sensitive, so this is fetchable by anyone.
/// </summary>
/// <param name="Id">The event group id.</param>
public sealed record GetEventGroupQuery(Guid Id) : IRequest<EventGroupResponse?>;
