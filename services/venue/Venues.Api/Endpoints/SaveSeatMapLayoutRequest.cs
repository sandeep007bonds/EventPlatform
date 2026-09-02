namespace Venues.Api.Endpoints;

/// <summary>
/// Request body for replacing the open draft's whole layout.
/// </summary>
/// <remarks>
/// The whole plan every time, not a patch — see <see cref="Venues.Domain.SeatMapLayout"/> for why.
/// For a large stadium this body is genuinely large, which is the honest cost of the editor holding
/// the plan and the server holding no session state about it.
/// </remarks>
/// <param name="Sections">Reserved-seating sections.</param>
/// <param name="AdmissionAreas">Unreserved capacity areas.</param>
/// <param name="Elements">The graphical layer.</param>
public sealed record SaveSeatMapLayoutRequest(
    IReadOnlyList<SeatMapSectionRequest>? Sections,
    IReadOnlyList<SeatMapAdmissionAreaRequest>? AdmissionAreas,
    IReadOnlyList<SeatMapElementRequest>? Elements);
