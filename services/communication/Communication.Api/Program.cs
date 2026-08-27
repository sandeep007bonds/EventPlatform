var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks.
builder.AddServiceDefaults(serviceName: "communication");

// Communication layers.
builder.Services.AddCommunicationApplication();
builder.Services.AddCommunicationInfrastructure(builder.Configuration);

// Registered after the senders so it observes whatever the config gates above actually resolved.
builder.Services.AddHostedService<NotificationSenderStartupLog>();

var app = builder.Build();

// Schema is applied by an explicit, separate step, never as a side effect of starting up:
// this same image applies migrations and exits when run with `--migrate`, and otherwise
// serves traffic without ever touching the schema (ADR-0029).
if (MigrationRunner.IsMigrationRun(args))
{
    await MigrationRunner.ApplyMigrationsAsync<CommunicationDbContext>(app.Services);
    return;
}

app.UseServiceDefaults();

// Dapr: unwrap the CloudEvent envelope so the topic handlers bind the event payload directly.
app.UseCloudEvents();

app.MapNotificationsEndpoints();

// Dapr: expose the subscription registration endpoint (/dapr/subscribe). AllowAnonymous
// deliberately — the sidecar fetches this manifest with no user token, and a denied fetch
// means no subscriptions are ever registered: pub/sub fails silently, not loudly.
app.MapSubscribeHandler().AllowAnonymous();

await app.RunAsync();
