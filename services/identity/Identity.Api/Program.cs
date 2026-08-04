var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(serviceName: "identity");

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseServiceDefaults();

// No app.UseCloudEvents() / app.MapSubscribeHandler() — Identity has zero Dapr pub/sub
// subscriptions this pass (it only makes one outbound Dapr service-invocation call, to
// Communication, from Identity.Infrastructure's DaprOtpSender).

app.MapOtpEndpoints();
app.MapOrganizerEndpoints();
app.MapDiscoveryEndpoints();

// DEV ONLY: create the schema from the current model on startup, so the service runs against
// local Postgres with zero setup. EnsureCreated, not Migrate — fine for disposable local dev, but
// cannot evolve an existing schema. Shared/deployed environments apply real EF Core migrations.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

await app.RunAsync();
