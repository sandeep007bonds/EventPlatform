var builder = WebApplication.CreateBuilder(args);

// Shared defaults: auth, OpenAPI/Scalar, JSON, OpenTelemetry, health checks.
builder.AddServiceDefaults(serviceName: "ordering");

// Ordering layers.
builder.Services.AddOrderingApplication();
builder.Services.AddOrderingInfrastructure(builder.Configuration);

// Durable checkout + cancellation sagas (Dapr Workflow).
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

    options.RegisterWorkflow<CancelOrderWorkflow>();
    options.RegisterActivity<FetchOrderActivity>();
    options.RegisterActivity<VoidTicketsActivity>();
    options.RegisterActivity<ReleaseSoldActivity>();
    options.RegisterActivity<MarkOrderRefundedActivity>();
});

var app = builder.Build();

app.UseServiceDefaults();
app.MapOrderingEndpoints();

// DEV ONLY: create the schema from the current model on startup, so the service runs against local
// Postgres with zero setup — no `dotnet ef` command, no committed Migrations/ folder. This is
// EnsureCreated, not Migrate: fine for local dev where the DB is disposable, but it cannot evolve an
// existing schema. Shared/deployed environments apply real EF Core migrations via a separate step.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

await app.RunAsync();
