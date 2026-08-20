namespace Ordering.Infrastructure;

/// <summary>
/// Talks to the Inventory service (app-id <c>inventory</c>) over Dapr service invocation, behind
/// the <see cref="IHoldClient"/> port used by the checkout saga.
/// </summary>
internal sealed class DaprHoldClient : IHoldClient
{
    private const string InventoryAppId = "inventory";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<HoldSnapshot?> GetHoldAsync(Guid holdId, CancellationToken cancellationToken)
    {
        using var http = DaprClient.CreateInvokeHttpClient(InventoryAppId);
        // The /snapshot twin, not the buyer-facing GET: this call carries no user token, and the
        // public endpoint requires the owning buyer (ADR-0035).
        using var response = await http.GetAsync($"v1/holds/{holdId}/snapshot", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var hold = await response.Content.ReadFromJsonAsync<InventoryHold>(JsonOptions, cancellationToken);
        if (hold is null)
        {
            return null;
        }

        var lines = hold.Lines
            .Select(line => new HoldLineSnapshot(
                line.InventoryItemId,
                line.SeatId,
                line.GeneralAdmissionAllocationId,
                line.Quantity,
                line.PriceTier,
                line.UnitPriceMinor,
                line.PriceMinor))
            .ToList();

        return new HoldSnapshot(
            hold.HoldId,
            hold.TenantId,
            hold.CatalogEventId,
            hold.UserId,
            hold.Status,
            hold.ExpiresAt,
            hold.TotalMinor,
            lines);
    }

    /// <inheritdoc />
    public async Task<bool> ConvertAsync(Guid holdId, Guid orderId, CancellationToken cancellationToken)
    {
        using var http = DaprClient.CreateInvokeHttpClient(InventoryAppId);
        using var response = await http.PostAsJsonAsync(
            $"v1/holds/{holdId}/convert",
            new { orderId },
            JsonOptions,
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<bool> ReleaseAsync(Guid holdId, CancellationToken cancellationToken)
    {
        using var http = DaprClient.CreateInvokeHttpClient(InventoryAppId);
        using var response = await http.PostAsync($"v1/holds/{holdId}/release", content: null, cancellationToken);

        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<DateTimeOffset?> ExtendAsync(Guid holdId, CancellationToken cancellationToken)
    {
        using var http = DaprClient.CreateInvokeHttpClient(InventoryAppId);
        using var response = await http.PostAsync($"v1/holds/{holdId}/extend", content: null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ExtendHoldResponse>(JsonOptions, cancellationToken);
        return body?.ExpiresAt;
    }

    /// <inheritdoc />
    public async Task<bool> CancelSoldAsync(Guid holdId, Guid orderId, CancellationToken cancellationToken)
    {
        using var http = DaprClient.CreateInvokeHttpClient(InventoryAppId);
        using var response = await http.PostAsJsonAsync(
            $"v1/holds/{holdId}/cancel",
            new { orderId },
            JsonOptions,
            cancellationToken);

        return response.IsSuccessStatusCode;
    }
}
