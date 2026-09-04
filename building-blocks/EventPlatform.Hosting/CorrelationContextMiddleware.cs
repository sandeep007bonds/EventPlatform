namespace EventPlatform.Hosting;

/// <summary>
/// Joins the current request to a chain of work: adopts an inbound <c>X-Correlation-Id</c> where
/// the caller supplied one, mints a fresh id where they did not, and echoes it back on the response.
/// </summary>
/// <remarks>
/// Mirrors <see cref="TenantContextMiddleware"/> deliberately — one scoped holder, one middleware
/// that fills it, registered next to each other.
/// <para>
/// The echo is what makes the id usable by a person: a buyer with a failing checkout can read the
/// id off the response (or off the error page, since it lands in ProblemDetails too) and hand it to
/// support, who can then find every row in every service that the attempt touched.
/// </para>
/// <para>
/// The id is also stamped onto the current <see cref="System.Diagnostics.Activity"/>, so a trace
/// and the durable record point at each other. They are not the same thing and neither replaces the
/// other: the trace expires and is sampled, this does not.
/// </para>
/// </remarks>
/// <param name="next">The next middleware.</param>
internal sealed class CorrelationContextMiddleware(RequestDelegate next)
{
    /// <summary>The request and response header carrying the id.</summary>
    internal const string HeaderName = "X-Correlation-Id";

    /// <summary>The tag the id is recorded under on the current activity.</summary>
    private const string ActivityTagName = "eventplatform.correlation_id";

    /// <summary>Adopts or mints the correlation id for this request.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="correlation">The scoped correlation holder.</param>
    /// <returns>A task that completes when the pipeline has run.</returns>
    public async Task InvokeAsync(HttpContext context, CorrelationContext correlation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(correlation);

        // A caller-supplied id is trusted only as an opaque grouping key, never as an authorization
        // or tenancy input — nothing reads it to decide access, so a forged one can only muddle a
        // trail the forger already appears in. An unparseable one is ignored rather than rejected:
        // failing a checkout over a malformed diagnostic header would be a worse trade.
        if (Guid.TryParse(context.Request.Headers[HeaderName], out var inbound))
        {
            correlation.Adopt(inbound, causation: null);
        }

        var correlationId = correlation.CorrelationId;
        context.Response.Headers[HeaderName] = correlationId.ToString();
        Activity.Current?.SetTag(ActivityTagName, correlationId);

        await next(context);
    }
}
