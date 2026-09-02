var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks.
builder.AddServiceDefaults(serviceName: "venue");

// Venue layers.
builder.Services.AddVenuesApplication();
builder.Services.AddVenuesInfrastructure(builder.Configuration);

var app = builder.Build();

// Schema is applied by an explicit, separate step, never as a side effect of starting up:
// this same image applies migrations and exits when run with `--migrate`, and otherwise
// serves traffic without ever touching the schema (ADR-0029).
if (MigrationRunner.IsMigrationRun(args))
{
    await MigrationRunner.ApplyMigrationsAsync<VenuesDbContext>(app.Services);
    return;
}

app.UseServiceDefaults();
app.MapVenueEndpoints();
app.MapSeatMapEndpoints();

await app.RunAsync();
