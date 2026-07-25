var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks.
builder.AddServiceDefaults(serviceName: "inventory");

// Inventory layers.
builder.Services.AddInventoryApplication();
builder.Services.AddInventoryInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();

// Dapr: unwrap the CloudEvent envelope so the topic handler binds the event payload directly.
app.UseCloudEvents();

app.MapInventoryEndpoints();

// Dapr: expose the subscription registration endpoint (/dapr/subscribe).
app.MapSubscribeHandler();

// DEV ONLY: apply EF Core migrations on startup so the service runs against local Postgres out of
// the box. In shared/deployed environments migrations are applied by a separate step, not the host.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();
