namespace Payments.Infrastructure;

/// <summary>EF Core implementation of <see cref="IPaymentRepository"/>.</summary>
/// <param name="dbContext">The Payments database context.</param>
internal sealed class PaymentRepository(PaymentDbContext dbContext) : IPaymentRepository
{
    /// <inheritdoc />
    public void Add(Payment payment) => dbContext.Payments.Add(payment);

    /// <inheritdoc />
    public Task<Payment?> GetByOrderAndKeyAsync(Guid orderId, string idempotencyKey, CancellationToken cancellationToken) =>
        dbContext.Payments.FirstOrDefaultAsync(
            p => p.OrderId == orderId && p.IdempotencyKey == idempotencyKey,
            cancellationToken);

    /// <inheritdoc />
    public Task<Payment?> GetCapturedByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Payments.FirstOrDefaultAsync(
            p => p.OrderId == orderId && p.Status == PaymentStatus.Captured,
            cancellationToken);

    /// <inheritdoc />
    public Task<Payment?> GetLatestByOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Payments
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetStaleInitiatedOrderIdsAsync(
        DateTimeOffset createdBefore,
        int batchSize,
        CancellationToken cancellationToken) =>
        await dbContext.Payments
            .Where(p => p.Status == PaymentStatus.Initiated && p.CreatedAt < createdBefore)
            .OrderBy(p => p.CreatedAt)
            .Take(batchSize)
            .Select(p => p.OrderId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<Payment?> GetByProviderReferenceAsync(string providerReference, CancellationToken cancellationToken) =>
        dbContext.Payments.FirstOrDefaultAsync(p => p.ProviderReference == providerReference, cancellationToken);

    /// <inheritdoc />
    public Task<bool> HasProcessedWebhookAsync(string eventId, CancellationToken cancellationToken) =>
        dbContext.ProcessedWebhookEvents.AnyAsync(e => e.EventId == eventId, cancellationToken);

    /// <inheritdoc />
    public void RecordProcessedWebhook(string eventId, string eventType) =>
        dbContext.ProcessedWebhookEvents.Add(ProcessedWebhookEvent.Record(eventId, eventType));

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // A concurrent charge for the same (order, idempotency key) won the race. Drop this
            // attempt's pending changes so the context is clean and let the caller re-fetch.
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }
}
