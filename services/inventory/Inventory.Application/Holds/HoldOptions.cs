namespace Inventory.Application.Holds;

/// <summary>Options controlling seat holds.</summary>
public sealed class HoldOptions
{
    /// <summary>
    /// How long a hold lives before it expires, while the buyer is still browsing/placing it.
    /// Kept short to protect turnover under contention. Defaults to two minutes.
    /// </summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// How long a hold is extended to once checkout submits and payment authentication begins
    /// (e.g. a 3D Secure challenge or a UPI app-switch) — deliberately longer than <see cref="Ttl"/>,
    /// since losing seats after the buyer has committed to paying is a far worse outcome than a
    /// strict pre-payment countdown. Defaults to fifteen minutes.
    /// </summary>
    public TimeSpan PaymentExtensionTtl { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Maximum seats a single hold may contain. Defaults to 10.</summary>
    public int MaxSeatsPerHold { get; set; } = 10;

    /// <summary>Maximum total general-admission quantity a single hold may contain. Defaults to 10.</summary>
    public int MaxGeneralAdmissionQuantityPerHold { get; set; } = 10;
}
