var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks.
builder.AddServiceDefaults(serviceName: "queue");

// Queue layers.
builder.Services.AddQueueApplication();
builder.Services.AddQueueInfrastructure(builder.Configuration);

var app = builder.Build();

// Schema is applied by an explicit, separate step, never as a side effect of starting up:
// this same image applies migrations and exits when run with `--migrate`, and otherwise
// serves traffic without ever touching the schema (ADR-0029).
if (MigrationRunner.IsMigrationRun(args))
{
    await MigrationRunner.ApplyMigrationsAsync<QueueDbContext>(app.Services);
    return;
}

// The join rate limiter buckets by the caller's address, and this service only ever sees traffic
// through the gateway — without this every buyer would share the gateway's address as one bucket
// and a handful of joins would lock out the whole waiting room. ForwardedHeaders replaces
// RemoteIpAddress with the client address YARP forwarded.
//
// KnownNetworks/KnownProxies are cleared because the gateway's pod address is not knowable ahead of
// time in Kubernetes. The trade-off is explicit: anything that can reach this service directly,
// bypassing the gateway, can set X-Forwarded-For and so pick its own rate-limit bucket. That is
// acceptable only because the limiter is abuse mitigation rather than a security control, and
// because in-cluster traffic is not attacker-reachable — do not extend this to anything that
// grants access on the strength of the caller's address (ADR-0026).
var forwardedHeaders = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor };
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);

app.UseServiceDefaults();

// Dapr: unwrap the CloudEvent envelope so the topic handler binds the event payload directly.
app.UseCloudEvents();

app.MapQueueEndpoints();

// Dapr: expose the subscription registration endpoint (/dapr/subscribe).
app.MapSubscribeHandler();

await app.RunAsync();
