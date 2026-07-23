using EventPlatform.Hosting;

var builder = WebApplication.CreateBuilder(args);

// One call gives this service the standard auth, OpenAPI, JSON, observability and health wiring.
builder.AddServiceDefaults(serviceName: "catalog");

var app = builder.Build();

app.UseServiceDefaults();

// Placeholder endpoint — replaced by the real Catalog slices during Phase 1 (issue #6).
// Anonymous for now so the skeleton runs without an identity provider configured.
app.MapGet("/v1/events/{id:guid}", (Guid id) =>
        Results.Ok(new { id, title = "TBD", status = "draft" }))
    .WithName("GetEvent")
    .AllowAnonymous();

app.Run();
