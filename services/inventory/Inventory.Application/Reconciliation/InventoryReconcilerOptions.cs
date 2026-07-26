namespace Inventory.Application.Reconciliation;

/// <summary>Options for the Redis↔Postgres drift reconciler.</summary>
public sealed class InventoryReconcilerOptions
{
    /// <summary>
    /// How often the reconciler checks whether the Redis fast gate needs rebuilding from Postgres.
    /// Defaults to thirty seconds — a flushed Redis is corrected within one interval.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
}
