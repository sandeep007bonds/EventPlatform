var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(serviceName: "identity");

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

var app = builder.Build();

// Schema is applied by an explicit, separate step, never as a side effect of starting up:
// this same image applies migrations and exits when run with `--migrate`, and otherwise
// serves traffic without ever touching the schema (ADR-0029).
if (MigrationRunner.IsMigrationRun(args))
{
    await MigrationRunner.ApplyMigrationsAsync<IdentityDbContext>(app.Services);
    return;
}

app.UseServiceDefaults();

// No app.UseCloudEvents() / app.MapSubscribeHandler() — Identity has zero Dapr pub/sub
// subscriptions this pass (it only makes one outbound Dapr service-invocation call, to
// Communication, from Identity.Infrastructure's DaprOtpSender).

app.MapOtpEndpoints();
app.MapOrganizerEndpoints();
app.MapDiscoveryEndpoints();

await app.RunAsync();
