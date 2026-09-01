namespace Catalog.Application.Features.SetPolicyDocument;

/// <summary>Result of attempting to write a policy document.</summary>
public enum SetPolicyDocumentOutcome
{
    /// <summary>The document was created or revised.</summary>
    Saved,

    /// <summary>The command scoped the document to an event that does not belong to the caller's tenant.</summary>
    EventNotFound,

    /// <summary>Nothing survived sanitising — the body was markup with no readable content.</summary>
    BodyEmptyAfterSanitising,
}
