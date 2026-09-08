namespace Catalog.Api.Endpoints;

/// <summary>Request body for setting which block is sold as which ticket type, for a performance.</summary>
/// <param name="Allocations">
/// The complete allocation map. Every block in the seat-map version must appear before the
/// performance can be published — either priced, or explicitly excluded. A block nobody decided
/// about is capacity nobody can buy and a hole in the map nobody can explain.
/// </param>
public sealed record SetSessionAllocationsRequest(IReadOnlyList<SessionAllocationRequest>? Allocations);
