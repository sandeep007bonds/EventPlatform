var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks.
builder.AddServiceDefaults(serviceName: "communication");

// Communication layers.
builder.Services.AddCommunicationApplication();
builder.Services.AddCommunicationInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();

// Dapr: unwrap the CloudEvent envelope so the topic handlers bind the event payload directly.
app.UseCloudEvents();

app.MapNotificationsEndpoints();

// Dapr: expose the subscription registration endpoint (/dapr/subscribe).
app.MapSubscribeHandler();

// DEV ONLY: create the schema from the current model on startup, so the service runs against local
// Postgres with zero setup — no `dotnet ef` command, no committed Migrations/ folder. This is
// EnsureCreated, not Migrate: fine for local dev where the DB is disposable, but it cannot evolve an
// existing schema. Shared/deployed environments apply real EF Core migrations via a separate step.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

await app.RunAsync();
