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
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
