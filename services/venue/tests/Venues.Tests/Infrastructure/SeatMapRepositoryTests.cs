namespace Venues.Tests.Infrastructure;

/// <summary>
/// Which version <see cref="SeatMapRepository.GetWithVersionAsync"/> hands back, against a real
/// Postgres container.
/// </summary>
/// <remarks>
/// The first database test this service has had, and it exists because the bug it pins could not
/// be seen any other way: the version filter is a query, so a substituted repository would have
/// agreed with itself. Published-only looked right until a map with nothing published yet — the
/// state every map is in the moment it is created — answered "not found" to its own owner, and
/// the seat-map editor could never open one.
/// </remarks>
public sealed class SeatMapRepositoryTests : IAsyncLifetime
{
    private static readonly Guid TenantId = Guid.CreateVersion7();
    private static readonly Guid VenueId = Guid.CreateVersion7();

    // Pinned to the image docker-compose runs, for the reasons ProcessedNotificationEventTests
    // spells out. Keep this in step with docker-compose.yml.
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    private string connectionString = default!;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        connectionString = container.GetConnectionString();

        await using var dbContext = NewDbContext();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await container.DisposeAsync();

    [Fact]
    public async Task AMapWithNothingPublishedYet_ReturnsItsDraft()
    {
        var seatMap = SeatMap.Create(VenueId, TenantId, "Main");
        seatMap.SaveDraftLayout(LayoutBuilder.Simple());
        await SaveAsync(seatMap);

        var loaded = await ReadAsync(seatMap.Id, versionNumber: null);

        var version = loaded!.Versions.ShouldHaveSingleItem();
        version.Status.ShouldBe(SeatMapVersionStatus.Draft);
        version.VersionNumber.ShouldBe(1);
    }

    // Once something is live, that is what a caller means by "the seat map" — the draft is a
    // work in progress and a buyer choosing a seat must never be shown it.
    [Fact]
    public async Task AMapWithBothAPublishedVersionAndAnOpenDraft_ReturnsThePublishedOne()
    {
        var seatMap = SeatMap.Create(VenueId, TenantId, "Main");
        seatMap.SaveDraftLayout(LayoutBuilder.Simple());
        seatMap.PublishDraft(DateTimeOffset.UtcNow);
        seatMap.StartNewDraft();
        seatMap.SaveDraftLayout(LayoutBuilder.Simple(seatsPerRow: 9));
        await SaveAsync(seatMap);

        var loaded = await ReadAsync(seatMap.Id, versionNumber: null);

        var version = loaded!.Versions.ShouldHaveSingleItem();
        version.Status.ShouldBe(SeatMapVersionStatus.Published);
        version.VersionNumber.ShouldBe(1);
    }

    // A ticket sold two configurations ago names seats that only its own version can resolve, so
    // asking for one by number has to keep working after it has been superseded.
    [Fact]
    public async Task ASupersededVersion_IsStillReturnedWhenAskedForByNumber()
    {
        var seatMap = SeatMap.Create(VenueId, TenantId, "Main");
        seatMap.SaveDraftLayout(LayoutBuilder.Simple());
        seatMap.PublishDraft(DateTimeOffset.UtcNow);
        seatMap.StartNewDraft();
        seatMap.SaveDraftLayout(LayoutBuilder.Simple(seatsPerRow: 9));
        seatMap.PublishDraft(DateTimeOffset.UtcNow);
        await SaveAsync(seatMap);

        var loaded = await ReadAsync(seatMap.Id, versionNumber: 1);

        var version = loaded!.Versions.ShouldHaveSingleItem();
        version.Status.ShouldBe(SeatMapVersionStatus.Superseded);
        version.VersionNumber.ShouldBe(1);
    }

    [Fact]
    public async Task AMapThatDoesNotExist_IsReportedAsMissingRatherThanThrowing() =>
        (await ReadAsync(Guid.CreateVersion7(), versionNumber: null)).ShouldBeNull();

    private VenuesDbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<VenuesDbContext>().UseNpgsql(connectionString).Options);

    private async Task SaveAsync(SeatMap seatMap)
    {
        await using var dbContext = NewDbContext();
        dbContext.SeatMaps.Add(seatMap);
        await dbContext.SaveChangesAsync();
    }

    // A fresh context per read, deliberately. The repository's loaders are tracked queries and
    // rely on relationship fixup, so reading through the context that wrote the rows would find
    // every version already attached and pass no matter what the filter did.
    private async Task<SeatMap?> ReadAsync(Guid seatMapId, int? versionNumber)
    {
        await using var dbContext = NewDbContext();
        var repository = new SeatMapRepository(dbContext);

        return await repository.GetWithVersionAsync(seatMapId, versionNumber, CancellationToken.None);
    }
}
