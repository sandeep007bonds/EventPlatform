namespace EventPlatform.Auditing;

/// <summary>
/// The chain of work the current scope belongs to.
/// </summary>
/// <remarks>
/// Sits beside <see cref="IAuditContext"/> deliberately: that one says <b>who</b>, this one says
/// <b>which piece of work</b>. Both are per-scope ambient values with the same lifetime, and
/// AUD-007 asks for them on the same record.
/// <para>
/// The distinction between the two ids is the whole point. <see cref="CorrelationId"/> is constant
/// for everything downstream of one originating action — a buyer pressing Pay produces an order, a
/// payment, a sold seat, a ticket and an email, and all of them share it. <see cref="CausationId"/>
/// is the single message that directly triggered <i>this</i> scope, so the chain can be walked one
/// hop at a time rather than only seen as a flat set.
/// </para>
/// <para>
/// This is not W3C <c>traceparent</c>, and does not replace it. A trace lives as long as the
/// telemetry backend keeps it and is sampled; this is written to the database beside the row it
/// explains, so a question asked next quarter still has an answer (PLAT-015).
/// </para>
/// </remarks>
public interface ICorrelationContext
{
    /// <summary>
    /// The id shared by every piece of work descending from one originating action. Never empty —
    /// an unattributable message is the failure this exists to prevent, so a scope that arrives
    /// without one is given a fresh id rather than none.
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// The message that directly caused this scope, or <see langword="null"/> when the scope is the
    /// start of the chain (a person's HTTP request, a timer firing).
    /// </summary>
    Guid? CausationId { get; }
}
