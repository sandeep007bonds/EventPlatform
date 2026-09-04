namespace EventPlatform.Auditing;

/// <summary>
/// Scoped, mutable holder for the current chain of work, populated by
/// <c>CorrelationContextMiddleware</c> from an inbound header, or by the integration-event
/// subscriber helper from a message's envelope.
/// </summary>
/// <remarks>
/// Mirrors <c>Hosting.TenantContext</c>'s shape — a plain scoped class with internal setters,
/// registered once and mutated by exactly one middleware. It self-seeds rather than starting empty:
/// a background service that never passes through the middleware still publishes events, and those
/// events must carry <i>some</i> correlation id, not a zero GUID that later reads as "unknown" in
/// every report.
/// </remarks>
public sealed class CorrelationContext : ICorrelationContext
{
    private Guid? correlationId;

    /// <inheritdoc />
    /// <remarks>
    /// Minted on first read when nothing has set one, so this property can never hand back
    /// <see cref="Guid.Empty"/>. Version 7 for the same reason ids are elsewhere in this codebase:
    /// time-sortable, so a correlation id is itself a rough timestamp when the row it is on has
    /// lost its own.
    /// </remarks>
    public Guid CorrelationId => correlationId ??= Guid.CreateVersion7();

    /// <inheritdoc />
    public Guid? CausationId { get; private set; }

    /// <summary>
    /// Adopts the chain of an inbound request or message.
    /// </summary>
    /// <param name="correlation">The chain to join. Ignored when empty, so a malformed header cannot blank it.</param>
    /// <param name="causation">The message that caused this scope, if any.</param>
    public void Adopt(Guid correlation, Guid? causation)
    {
        if (correlation != Guid.Empty)
        {
            correlationId = correlation;
        }

        CausationId = causation;
    }
}
