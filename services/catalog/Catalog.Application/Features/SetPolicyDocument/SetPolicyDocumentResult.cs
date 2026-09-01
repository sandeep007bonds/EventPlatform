namespace Catalog.Application.Features.SetPolicyDocument;

/// <summary>The outcome of a policy write, plus the version that is now in force.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="Version">
/// The document's version after the write, or <c>0</c> when nothing was written. Returned so a
/// caller can show it, and so an integration test can assert that an unchanged body does not
/// invalidate the version orders already point at.
/// </param>
public sealed record SetPolicyDocumentResult(SetPolicyDocumentOutcome Outcome, int Version);
