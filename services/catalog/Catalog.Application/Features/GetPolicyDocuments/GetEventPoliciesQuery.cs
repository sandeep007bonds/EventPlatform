namespace Catalog.Application.Features.GetPolicyDocuments;

/// <summary>
/// Query for the policy documents a buyer sees on one event's page, without knowing which tenant
/// owns it.
/// </summary>
/// <remarks>
/// Separate from <see cref="GetPolicyDocumentsQuery"/> because the tenant is derived from the event
/// rather than supplied: this is the anonymous read, and letting a caller name the tenant would let
/// them read one organizer's documents through another organizer's event.
/// </remarks>
/// <param name="EventId">The event.</param>
/// <param name="CallerTenantId">The caller's tenant id, or <see langword="null"/> for an anonymous caller.</param>
public sealed record GetEventPoliciesQuery(Guid EventId, Guid? CallerTenantId)
    : IRequest<IReadOnlyList<PolicyDocumentResponse>?>;
