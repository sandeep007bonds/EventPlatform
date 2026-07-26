var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks.
builder.AddServiceDefaults(serviceName: "ticketing");

// Ticketing layers.
builder.Services.AddTicketingApplication();
builder.Services.AddTicketingInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();

// Dapr: unwrap the CloudEvent envelope so the topic handler binds the event payload directly.
app.UseCloudEvents();

app.MapTicketingEndpoints();

// Dapr: expose the subscription registration endpoint (/dapr/subscribe).
app.MapSubscribeHandler();

// DEV ONLY: create the schema from the current model on startup, so the service runs against local
// Postgres with zero setup — no `dotnet ef` command, no committed Migrations/ folder. This is
// EnsureCreated, not Migrate: fine for local dev where the DB is disposable, but it cannot evolve an
// existing schema. Shared/deployed environments apply real EF Core migrations via a separate step.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

await app.RunAsync();
