var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks.
builder.AddServiceDefaults(serviceName: "ordering");

// Ordering layers.
builder.Services.AddOrderingApplication();
builder.Services.AddOrderingInfrastructure(builder.Configuration);

// Durable checkout saga (Dapr Workflow).
builder.Services.AddDaprWorkflow(options =>
{
    options.RegisterWorkflow<CheckoutWorkflow>();
    options.RegisterActivity<FetchHoldActivity>();
    options.RegisterActivity<CreateOrderActivity>();
    options.RegisterActivity<ChargeActivity>();
    options.RegisterActivity<ConvertActivity>();
    options.RegisterActivity<ConfirmOrderActivity>();
    options.RegisterActivity<ReleaseHoldActivity>();
    options.RegisterActivity<RefundActivity>();
    options.RegisterActivity<FailOrderActivity>();
});

var app = builder.Build();

app.UseServiceDefaults();
app.MapOrderingEndpoints();

// DEV ONLY: apply EF Core migrations on startup so the service runs against local Postgres out of
// the box. In shared/deployed environments migrations are applied by a separate step, not the host.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();
