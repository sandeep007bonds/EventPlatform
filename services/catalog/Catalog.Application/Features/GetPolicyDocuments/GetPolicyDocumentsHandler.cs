namespace Catalog.Application.Features.GetPolicyDocuments;

/// <summary>
/// Handles <see cref="GetPolicyDocumentsQuery"/>, applying the override-wins rule so a caller
/// receives exactly one document per kind.
/// </summary>
/// <param name="repository">The policy-document repository.</param>
internal sealed class GetPolicyDocumentsHandler(IPolicyDocumentRepository repository)
    : IRequestHandler<GetPolicyDocumentsQuery, IReadOnlyList<PolicyDocumentResponse>>
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PolicyDocumentResponse>> Handle(
        GetPolicyDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var documents = request.EventId is { } eventId
            ? await repository.ListForEventAsync(request.TenantId, eventId, cancellationToken)
            : await repository.ListDefaultsAsync(request.TenantId, cancellationToken);

        // One row per kind, the event's own winning over the tenant default. Ordering by EventId
        // descending would rely on a GUID comparison to mean "override"; grouping and picking the
        // scoped one explicitly says what is intended.
        return documents
            .GroupBy(document => document.Kind)
            .Select(group => group.FirstOrDefault(document => document.EventId is not null) ?? group.First())
            .OrderBy(document => document.Kind)
            .Select(document => new PolicyDocumentResponse(
                document.Kind.ToString(),
                document.BodyHtml,
                document.Version,
                document.UpdatedAt,
                document.EventId is not null))
            .ToList();
    }
}
