namespace Ordering.Workflow;

/// <summary>
/// External event payload raised by Ordering's payment webhook subscriber
/// (<c>OnPaymentCapturedAsync</c>/<c>OnPaymentFailedAsync</c>) into the waiting checkout saga.
/// </summary>
/// <param name="Captured">Whether the payment was captured (vs. declined/failed).</param>
/// <param name="FailureReason">Why the payment failed, when <paramref name="Captured"/> is <see langword="false"/>.</param>
public sealed record PaymentOutcomeSignal(bool Captured, string? FailureReason);
