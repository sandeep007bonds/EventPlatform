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

// DEV ONLY: apply EF Core migrations on startup so the service runs against local Postgres out of
// the box. In shared/deployed environments migrations are applied by a separate step, not the host.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TicketingDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();
