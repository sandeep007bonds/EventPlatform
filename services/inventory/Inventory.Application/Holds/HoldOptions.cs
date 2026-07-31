namespace Inventory.Application.Holds;

/// <summary>Options controlling seat holds.</summary>
public sealed class HoldOptions
{
    /// <summary>How long a hold lives before it expires. Defaults to two minutes.</summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Maximum seats a single hold may contain. Defaults to 10.</summary>
    public int MaxSeatsPerHold { get; set; } = 10;

    /// <summary>Maximum total general-admission quantity a single hold may contain. Defaults to 10.</summary>
    public int MaxGeneralAdmissionQuantityPerHold { get; set; } = 10;
}
