namespace Catalog.Infrastructure;

/// <summary>EF Core implementation of <see cref="IEventRepository"/>.</summary>
/// <param name="dbContext">The Catalog database context.</param>
internal sealed class EventRepository(CatalogDbContext dbContext) : IEventRepository
{
    /// <inheritdoc />
    public void Add(Event @event) => dbContext.Events.Add(@event);

    /// <inheritdoc />
    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
