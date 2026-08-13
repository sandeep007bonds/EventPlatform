namespace Ordering.Api.Endpoints;

/// <summary>Response for a buyer-triggered payment reconciliation.</summary>
/// <param name="Status">
/// The payment's state after reconciliation — one of <c>NotFound</c>, <c>Pending</c>,
/// <c>Captured</c> or <c>Failed</c>. Informational only: the order itself is confirmed by the
/// checkout saga once it observes the resulting <c>PaymentCaptured</c> event, so the browser should
/// still poll the order rather than treat <c>Captured</c> here as the order being ready.
/// </param>
public sealed record PaymentSyncStatusResponse(string Status);
