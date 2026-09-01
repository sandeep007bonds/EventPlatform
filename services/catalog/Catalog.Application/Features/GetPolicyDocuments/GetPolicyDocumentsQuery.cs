namespace Catalog.Application.Features.GetPolicyDocuments;

/// <summary>
/// Query for the policy documents in force — for one event (overrides resolved against the
/// organizer's defaults) or, with no event, the tenant's defaults on their own.
/// </summary>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="EventId">The event, or <see langword="null"/> for the tenant's own defaults.</param>
public sealed record GetPolicyDocumentsQuery(Guid TenantId, Guid? EventId)
    : IRequest<IReadOnlyList<PolicyDocumentResponse>>;
