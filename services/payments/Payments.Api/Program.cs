var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks.
builder.AddServiceDefaults(serviceName: "payments");

// Payments layers.
builder.Services.AddPaymentsApplication();
builder.Services.AddPaymentsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.MapPaymentsEndpoints();

// DEV ONLY: create the schema from the current model on startup, so the service runs against local
// Postgres with zero setup — no `dotnet ef` command, no committed Migrations/ folder. This is
// EnsureCreated, not Migrate: fine for local dev where the DB is disposable, but it cannot evolve an
// existing schema. Shared/deployed environments apply real EF Core migrations via a separate step.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

await app.RunAsync();
