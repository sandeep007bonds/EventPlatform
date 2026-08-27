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
    options.RegisterActivity<FetchEventPricingActivity>();
    options.RegisterActivity<EvaluatePromoCodeActivity>();
    options.RegisterActivity<CreateOrderActivity>();
    options.RegisterActivity<CreateIntentActivity>();
    options.RegisterActivity<RecordPaymentIntentActivity>();
    options.RegisterActivity<ExtendHoldActivity>();
    options.RegisterActivity<SyncPaymentStatusActivity>();
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

// Schema is applied by an explicit, separate step, never as a side effect of starting up:
// this same image applies migrations and exits when run with `--migrate`, and otherwise
// serves traffic without ever touching the schema (ADR-0029).
if (MigrationRunner.IsMigrationRun(args))
{
    await MigrationRunner.ApplyMigrationsAsync<OrderingDbContext>(app.Services);
    return;
}

app.UseServiceDefaults();
app.UseCloudEvents();
app.MapOrderingEndpoints();

// Dapr: expose the subscription registration endpoint (/dapr/subscribe). AllowAnonymous
// deliberately — the sidecar fetches this manifest with no user token, and a denied fetch
// means no subscriptions are ever registered: pub/sub fails silently, not loudly.
app.MapSubscribeHandler().AllowAnonymous();

await app.RunAsync();
