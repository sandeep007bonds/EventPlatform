namespace Venues.Application.Features.GetSeatMap;

/// <summary>
/// Handles <see cref="GetSeatMapQuery"/>.
/// </summary>
/// <remarks>
/// A <b>published or superseded</b> version is readable by anyone: buyers have to render the plan
/// to choose a seat, and a ticket sold under an older version has to keep resolving. A
/// <b>draft</b> is readable only by the tenant that owns it — an unannounced reconfiguration is
/// exactly the kind of thing a competitor would like to see early.
/// </remarks>
/// <param name="repository">The seat-map repository.</param>
internal sealed class GetSeatMapHandler(ISeatMapRepository repository)
    : IRequestHandler<GetSeatMapQuery, SeatMapResponse?>
{
    /// <inheritdoc />
    public async Task<SeatMapResponse?> Handle(GetSeatMapQuery request, CancellationToken cancellationToken)
    {
        var seatMap = await repository.GetWithVersionAsync(
            request.SeatMapId,
            request.VersionNumber,
            cancellationToken);

        var version = seatMap?.Versions.SingleOrDefault();
        if (seatMap is null || version is null)
        {
            return null;
        }

        var isOwner = request.TenantId == seatMap.TenantId;
        if (version.Status == SeatMapVersionStatus.Draft && !isOwner)
        {
            return null;
        }

        return seatMap.ToResponse(version);
    }
}
