var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks.
builder.AddServiceDefaults(serviceName: "ticketing");

// Ticketing layers.
builder.Services.AddTicketingApplication();
builder.Services.AddTicketingInfrastructure(builder.Configuration);

var app = builder.Build();

// Schema is applied by an explicit, separate step, never as a side effect of starting up:
// this same image applies migrations and exits when run with `--migrate`, and otherwise
// serves traffic without ever touching the schema (ADR-0029).
if (MigrationRunner.IsMigrationRun(args))
{
    await MigrationRunner.ApplyMigrationsAsync<TicketingDbContext>(app.Services);
    return;
}

app.UseServiceDefaults();

// Dapr: unwrap the CloudEvent envelope so the topic handler binds the event payload directly.
app.UseCloudEvents();

app.MapTicketingEndpoints();

// Dapr: expose the subscription registration endpoint (/dapr/subscribe). AllowAnonymous
// deliberately — the sidecar fetches this manifest with no user token, and a denied fetch
// means no subscriptions are ever registered: pub/sub fails silently, not loudly.
app.MapSubscribeHandler().AllowAnonymous();

await app.RunAsync();
