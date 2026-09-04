namespace EventPlatform.Hosting;

/// <summary>
/// Registers the standard EventPlatform service defaults shared by every service:
/// JSON options, authentication, OpenAPI, observability, health checks and problem details.
/// This is the single entry point services use so every service is wired the same way.
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Adds the common EventPlatform service defaults to the application builder.
    /// Call once from a service's <c>Program.cs</c> before building the app.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="serviceName">The logical service name (used for telemetry and OpenAPI).</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        builder.Services.AddDefaultJsonOptions();
        builder.Services.AddEventPlatformAuthentication(builder.Configuration);
        builder.Services.AddEventPlatformOpenApi(serviceName);
        builder.AddDefaultObservability(serviceName);
        builder.Services.AddDefaultHealthChecks();

        // Unhandled exceptions always become a ProblemDetails response (never a raw 500 with no
        // body) - but the message/stack trace are only attached in Development. Without this,
        // UseExceptionHandler()'s default output is the same generic "An error occurred..." with
        // no Detail in every environment, which makes local debugging from the HTTP response alone
        // impossible. Never enable this outside Development - it would leak internals to callers.
        var environment = builder.Environment;
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                // In *every* environment, unlike the exception details below. This is the id a
                // person reads off a failure and quotes to support, so it has to be on the
                // production response — that is the whole of PLAT-015's ask. It leaks nothing:
                // it is an opaque grouping key that means something only to someone who can
                // already query the databases.
                context.ProblemDetails.Extensions["correlationId"] =
                    context.HttpContext.RequestServices.GetRequiredService<ICorrelationContext>().CorrelationId;

                if (!environment.IsDevelopment())
                {
                    return;
                }

                var exception = context.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception is null)
                {
                    return;
                }

                context.ProblemDetails.Detail = exception.Message;
                context.ProblemDetails.Extensions["stackTrace"] = exception.StackTrace;
            };
        });

        builder.Services.AddHttpContextAccessor();

        // The audit actor, scoped per request. serviceName is already a parameter here, so every
        // service gets a correct identity for its own background writes without configuring
        // anything — which is what keeps saga and reaper writes from being recorded as nobody.
        builder.Services.AddScoped<IAuditContext>(sp =>
            new HttpAuditContext(sp.GetRequiredService<IHttpContextAccessor>(), serviceName));

        // The chain of work, beside the actor. TryAdd because AddOutbox registers the same pair —
        // a service can call either or both, and whichever runs first should win rather than the
        // second silently replacing a context the first already populated.
        builder.Services.TryAddScoped<CorrelationContext>();
        builder.Services.TryAddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());

        return builder;
    }

    /// <summary>
    /// Wires the common middleware pipeline for an EventPlatform service:
    /// exception handling (problem details), authentication, tenant resolution,
    /// authorization, OpenAPI and health-check endpoints.
    /// Call once after <see cref="WebApplicationBuilder.Build"/>.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static WebApplication UseServiceDefaults(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseAuthentication();

        // Before tenant resolution and authorization, so an id exists for anything they reject —
        // a 401 or a 403 is exactly the response a person is most likely to be asking about.
        app.UseMiddleware<CorrelationContextMiddleware>();
        app.UseMiddleware<TenantContextMiddleware>();
        app.UseAuthorization();
        app.UseEventPlatformOpenApi();
        app.MapDefaultHealthChecks();

        return app;
    }
}
