namespace Ordering.Workflow;

/// <summary>Input to the record-payment-intent activity.</summary>
/// <param name="OrderId">The order to record the client secret on.</param>
/// <param name="ClientSecret">The PSP client secret.</param>
public sealed record RecordPaymentIntentInput(Guid OrderId, string ClientSecret);
