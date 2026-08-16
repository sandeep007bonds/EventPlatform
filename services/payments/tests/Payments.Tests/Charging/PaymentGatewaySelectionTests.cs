namespace Payments.Tests.Charging;

// Which gateway Payments resolves is the difference between charging money and only pretending to,
// and it is decided entirely by configuration — so it is worth asserting directly, with no Stripe
// account involved. The assertions go through the public AddPaymentsInfrastructure gate and match on
// type name, because the gateway implementations are internal to Payments.Infrastructure.
public sealed class PaymentGatewaySelectionTests
{
    [Fact]
    public void NoStripeKeyConfigured_FallsBackToTheSimulator()
    {
        var gateway = ResolveGateway(stripeSecretKey: null);

        gateway.GetType().Name.ShouldBe("SimulatedPaymentGateway");
    }

    [Fact]
    public void RealStripeSecretKey_SelectsTheStripeGateway()
    {
        var gateway = ResolveGateway(stripeSecretKey: "sk_test_0123456789abcdef");

        gateway.GetType().Name.ShouldBe("StripePaymentGateway");
    }

    // The regression that matters most in a deployed environment: Key Vault always supplies a value
    // for this setting (the CSI SecretProviderClass lists the object by name, so it must exist), and
    // a non-empty check would hand Terraform's placeholder straight to Stripe and fail every
    // checkout. Only something shaped like a Stripe key counts as configured.
    [Theory]
    [InlineData("placeholder-set-me-with-az-keyvault-secret-set")]
    [InlineData("   ")]
    [InlineData("pk_test_0123456789abcdef")]
    public void AValueThatIsNotAStripeSecretKey_IsTreatedAsUnconfigured(string configured)
    {
        var gateway = ResolveGateway(configured);

        gateway.GetType().Name.ShouldBe("SimulatedPaymentGateway");
    }

    [Fact]
    public void NoWebhookSigningSecret_LeavesWebhookVerificationUnregistered()
    {
        using var provider = BuildProvider(stripeSecretKey: null, stripeWebhookSecret: null);

        provider.GetService<IPaymentWebhookGateway>().ShouldBeNull();
    }

    [Fact]
    public void RealWebhookSigningSecret_RegistersWebhookVerification()
    {
        using var provider = BuildProvider(stripeSecretKey: null, stripeWebhookSecret: "whsec_0123456789abcdef");

        provider.GetService<IPaymentWebhookGateway>().ShouldNotBeNull();
    }

    [Fact]
    public void APlaceholderWebhookSecret_LeavesWebhookVerificationUnregistered()
    {
        using var provider = BuildProvider(
            stripeSecretKey: null,
            stripeWebhookSecret: "placeholder-set-me-with-az-keyvault-secret-set");

        provider.GetService<IPaymentWebhookGateway>().ShouldBeNull();
    }

    private static IPaymentGateway ResolveGateway(string? stripeSecretKey)
    {
        using var provider = BuildProvider(stripeSecretKey, stripeWebhookSecret: null);

        return provider.GetRequiredService<IPaymentGateway>();
    }

    private static ServiceProvider BuildProvider(string? stripeSecretKey, string? stripeWebhookSecret)
    {
        var settings = new Dictionary<string, string?>
        {
            // Registration only builds the DbContext options; nothing connects.
            ["ConnectionStrings:payments"] = "Host=localhost;Database=payments;Username=u;Password=p",
            ["Payments:Stripe:SecretKey"] = stripeSecretKey,
            ["Payments:Stripe:WebhookSecret"] = stripeWebhookSecret,
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddLogging()
            .AddPaymentsInfrastructure(configuration)
            .BuildServiceProvider();
    }
}
