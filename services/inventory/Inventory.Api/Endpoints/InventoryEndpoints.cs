namespace Inventory.Api.Endpoints;

/// <summary>Maps the Inventory HTTP endpoints, including the Dapr pub/sub subscription.</summary>
public static class InventoryEndpoints
{
    /// <summary>Maps the Inventory endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Dapr pub/sub: provision seat inventory when Catalog publishes an event.
        app.MapPost("/integration/catalog/event-published", OnEventPublishedAsync)
            .WithTopic("pubsub", nameof(EventPublished))
            .WithName("OnEventPublished")
            .ExcludeFromDescription();

        app.MapGet("/v1/events/{eventId:guid}/inventory", GetInventoryCountAsync)
            .WithName("GetInventoryCount")
            .WithTags("Inventory");

        return app;
    }

    private static async Task<IResult> OnEventPublishedAsync(
        EventPublished @event,
        InventoryProvisioningService provisioning,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var result = await provisioning.ProvisionAsync(@event.TenantId, @event.CatalogEventId, cancellationToken);

        var logger = loggerFactory.CreateLogger("Inventory.Provisioning");
        if (result.Provisioned)
        {
            logger.LogInformation(
                "Provisioned {SeatCount} seats for event {EventId}.",
                result.SeatCount,
                @event.CatalogEventId);
        }
        else
        {
            logger.LogInformation("Event {EventId} already provisioned; skipped.", @event.CatalogEventId);
        }

        // Ack so Dapr does not redeliver; provisioning is idempotent if it does.
        return Results.Ok();
    }

    private static async Task<IResult> GetInventoryCountAsync(
        Guid eventId,
        IInventoryRepository repository,
        CancellationToken cancellationToken)
    {
        var count = await repository.CountForEventAsync(eventId, cancellationToken);
        return Results.Ok(new { eventId, seatCount = count });
    }
}
