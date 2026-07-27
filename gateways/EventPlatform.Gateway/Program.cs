var builder = WebApplication.CreateBuilder(args);

// The gateway is a stateless HTTP proxy, not a service with its own auth/tenant model — it
// deliberately does NOT call EventPlatform.Hosting's AddServiceDefaults/UseServiceDefaults (that
// bundles JWT validation + tenant-context middleware a proxy doesn't own). Instead it cherry-picks
// only the pieces that generically apply to any host.
builder.Services.AddDefaultJsonOptions();
builder.Services.AddEventPlatformOpenApi("gateway");
builder.AddDefaultObservability("gateway");
builder.Services.AddDefaultHealthChecks();
builder.Services.AddProblemDetails();

builder.Services.AddGatewayCors(builder.Configuration);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Fail fast rather than silently exposing a token-mint endpoint: DevSigningKey must never be
// configured in Production. Unlike a service that only *validates* tokens, the gateway *issues*
// them, so a misconfiguration here has a much bigger blast radius.
var devSigningKey = app.Configuration["Jwt:DevSigningKey"];
if (app.Environment.IsProduction() && !string.IsNullOrWhiteSpace(devSigningKey))
{
    throw new InvalidOperationException(
        "Jwt:DevSigningKey must not be configured in Production — it would expose the dev-only token-mint endpoint.");
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(CorsExtensions.FrontendPolicyName);
app.UseEventPlatformOpenApi();
app.MapDefaultHealthChecks();

// DEV ONLY: stands in for real login (Identity-service OTP for buyers, Entra External ID OIDC for
// organizers) until both exist. Only mapped when a dev signing key is configured, mirroring
// AuthenticationExtensions.cs's existing gating pattern in every other service.
if (!string.IsNullOrWhiteSpace(devSigningKey))
{
    app.MapDevAuthEndpoints();
}

app.MapReverseProxy();

await app.RunAsync();
