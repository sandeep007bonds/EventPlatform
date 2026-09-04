namespace Ticketing.Infrastructure;

/// <summary>A reserved-seating section, as far as Ticketing's gate map reads it.</summary>
/// <param name="GateId">The gate this section is entered through, or <see langword="null"/> for any.</param>
/// <param name="Rows">The section's rows.</param>
internal sealed record VenueScanSection(Guid? GateId, IReadOnlyList<VenueScanRow> Rows);
