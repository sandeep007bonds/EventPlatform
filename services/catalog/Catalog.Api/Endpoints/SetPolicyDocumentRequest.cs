namespace Catalog.Api.Endpoints;

/// <summary>Request body for writing a terms, privacy or refund document.</summary>
/// <param name="BodyHtml">The document body as HTML. Sanitised server-side before it is stored.</param>
public sealed record SetPolicyDocumentRequest(string BodyHtml);
