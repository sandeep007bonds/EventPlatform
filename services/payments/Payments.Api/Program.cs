var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks.
builder.AddServiceDefaults(serviceName: "payments");

// Payments layers.
builder.Services.AddPaymentsApplication();
builder.Services.AddPaymentsInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.MapPaymentsEndpoints();

// DEV ONLY: apply EF Core migrations on startup so the service runs against local Postgres out of
// the box. In shared/deployed environments migrations are applied by a separate step, not the host.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();
