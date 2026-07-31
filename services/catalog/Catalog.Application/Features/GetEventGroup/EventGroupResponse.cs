namespace Catalog.Application.Features.GetEventGroup;

/// <summary>Read model returned for a single event group.</summary>
/// <param name="Id">Event group id.</param>
/// <param name="Title">Group title.</param>
public sealed record EventGroupResponse(Guid Id, string Title);
