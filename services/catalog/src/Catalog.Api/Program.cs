using Catalog.Api.Endpoints;
using Catalog.Application;
using Catalog.Infrastructure;
using EventPlatform.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks.
builder.AddServiceDefaults(serviceName: "catalog");

// Catalog layers.
builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();
app.MapCatalogEndpoints();

// DEV ONLY: create the schema from the model so the service runs against local Postgres
// out of the box. Replaced by EF Core migrations before any shared/deployed environment.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

await app.RunAsync();
