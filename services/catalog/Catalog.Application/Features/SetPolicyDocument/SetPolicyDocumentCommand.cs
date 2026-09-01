namespace Catalog.Application.Features.SetPolicyDocument;

/// <summary>
/// Command to write a tenant's default policy document, or an event's override of one. Creates it
/// on first write and revises it (bumping the version) afterwards. <see cref="TenantId"/> is set
/// server-side from the validated JWT, per ADR-0011.
/// </summary>
/// <param name="TenantId">The owning tenant.</param>
/// <param name="EventId">The event to scope this to, or <see langword="null"/> for the tenant default.</param>
/// <param name="Kind">Which document.</param>
/// <param name="BodyHtml">The document body as HTML, as typed. Sanitised by the handler before it is stored.</param>
public sealed record SetPolicyDocumentCommand(Guid TenantId, Guid? EventId, PolicyKind Kind, string BodyHtml)
    : IRequest<SetPolicyDocumentResult>;
