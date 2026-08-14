namespace Payments.Application.Charging;

/// <summary>Options for the stale-payment reconciler.</summary>
public sealed class PaymentReconcilerOptions
{
    /// <summary>How often the reconciler scans for stale payments. Defaults to one minute.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How old an <c>Initiated</c> payment must be before it is treated as stale. Defaults to
    /// twenty minutes — comfortably past <c>HoldOptions.PaymentExtensionTtl</c> (15 minutes), so a
    /// buyer still working through a 3-D Secure challenge is never cut short by this sweep.
    /// </summary>
    public TimeSpan StaleAfter { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>Maximum payments reconciled per scan. Defaults to 100.</summary>
    public int BatchSize { get; set; } = 100;
}
