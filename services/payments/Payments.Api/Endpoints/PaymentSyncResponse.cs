namespace Payments.Api.Endpoints;

/// <summary>Response for a payment status re-read.</summary>
/// <param name="Status">
/// The payment's state after reconciliation — one of <c>NotFound</c>, <c>Pending</c>,
/// <c>Captured</c> or <c>Failed</c>.
/// </param>
public sealed record PaymentSyncResponse(string Status);
