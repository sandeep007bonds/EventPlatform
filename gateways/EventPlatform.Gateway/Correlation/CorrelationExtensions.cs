namespace EventPlatform.Gateway.Correlation;

/// <summary>
/// Gives every request that enters the platform a correlation id, at the first place it can be
/// given one.
/// </summary>
/// <remarks>
/// The backend services mint one too (<c>CorrelationContextMiddleware</c>), so this is not what
/// makes the id exist — it is what makes the id <b>the same</b> across the several calls one screen
/// makes. A checkout page that loads a hold, quotes a price and then pays would otherwise produce
/// three unrelated chains, and the buyer's complaint would match only one of them.
/// <para>
/// The gateway does not call <c>UseServiceDefaults</c> (see this project's README — that bundles
/// auth and tenant middleware a stateless proxy does not own), so this small piece stands alone
/// rather than reusing the shared one.
/// </para>
/// </remarks>
public static class CorrelationExtensions
{
    /// <summary>The request and response header carrying the id, matching the backend services.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>
    /// Mints a correlation id when the caller did not supply one, and echoes it on the response.
    /// </summary>
    /// <remarks>
    /// Written onto the <b>request</b> as well as the response: YARP forwards request headers
    /// downstream, so stamping it here is what makes the backend adopt this id rather than mint its
    /// own. Register before <c>MapReverseProxy</c>.
    /// </remarks>
    /// <param name="app">The web application.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static WebApplication UseCorrelationId(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Use(async (context, next) =>
        {
            // A caller-supplied id is trusted only as an opaque grouping key — nothing reads it to
            // decide access — so an unparseable one is replaced rather than rejected. Failing a
            // request over a malformed diagnostic header would be the worse trade.
            if (!Guid.TryParse(context.Request.Headers[HeaderName], out var correlationId))
            {
                correlationId = Guid.CreateVersion7();
                context.Request.Headers[HeaderName] = correlationId.ToString();
            }

            context.Response.Headers[HeaderName] = correlationId.ToString();
            await next(context);
        });

        return app;
    }
}
