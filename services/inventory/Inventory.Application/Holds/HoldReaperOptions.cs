namespace Inventory.Application.Holds;

/// <summary>Options for the expired-hold reaper.</summary>
public sealed class HoldReaperOptions
{
    /// <summary>How often the reaper scans for expired holds. Defaults to five seconds.</summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Maximum holds reclaimed per scan. Defaults to 200.</summary>
    public int BatchSize { get; set; } = 200;
}
