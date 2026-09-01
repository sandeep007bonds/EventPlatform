namespace Catalog.Application.Features.GetPolicyDocuments;

/// <summary>One resolved policy document.</summary>
/// <param name="Kind">Which document this is.</param>
/// <param name="BodyHtml">The sanitised body HTML.</param>
/// <param name="Version">The revision number in force.</param>
/// <param name="UpdatedAt">When it was last revised (UTC).</param>
/// <param name="IsEventOverride">
/// <see langword="true"/> when this event carries its own version of the document;
/// <see langword="false"/> when what is shown is the organizer's tenant-wide default.
/// </param>
public sealed record PolicyDocumentResponse(
    string Kind,
    string BodyHtml,
    int Version,
    DateTimeOffset UpdatedAt,
    bool IsEventOverride);
