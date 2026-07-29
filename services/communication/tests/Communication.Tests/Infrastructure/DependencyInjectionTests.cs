namespace Communication.Tests.Infrastructure;

/// <summary>
/// Proves requirement 1 (either vendor pluggable per channel via config, no live credentials
/// needed) structurally: resolves the DI container built from fake configuration and asserts which
/// concrete sender type came out.
/// </summary>
public sealed class DependencyInjectionTests
{
    private static readonly Dictionary<string, string?> BaseConfig = new()
    {
        ["ConnectionStrings:communication"] = "Host=localhost;Database=test_communication_di;Username=test;Password=test",
    };

    [Fact]
    public void NoProviderConfigured_ResolvesLoggingSenders()
    {
        var provider = BuildServiceProvider(BaseConfig);

        provider.GetRequiredService<IEmailSender>().ShouldBeOfType<LoggingEmailSender>();
        provider.GetRequiredService<ISmsSender>().ShouldBeOfType<LoggingSmsSender>();
        provider.GetRequiredService<IWhatsAppSender>().ShouldBeOfType<LoggingWhatsAppSender>();
    }

    [Fact]
    public void AcsConfigured_ResolvesAcsSenders()
    {
        var config = new Dictionary<string, string?>(BaseConfig)
        {
            ["Communication:Acs:ConnectionString"] = "endpoint=https://fake.communication.azure.com/;accesskey=ZmFrZQ==",
            ["Communication:Acs:EmailFromAddress"] = "no-reply@example.com",
            ["Communication:Acs:SmsFromNumber"] = "+15550000000",
            ["Communication:Email:Provider"] = "Acs",
            ["Communication:Sms:Provider"] = "Acs",
        };

        var provider = BuildServiceProvider(config);

        provider.GetRequiredService<IEmailSender>().ShouldBeOfType<AcsEmailSender>();
        provider.GetRequiredService<ISmsSender>().ShouldBeOfType<AcsSmsSender>();
    }

    [Fact]
    public void TwilioConfigured_ResolvesTwilioSmsAndWhatsAppSenders()
    {
        var config = new Dictionary<string, string?>(BaseConfig)
        {
            ["Communication:Twilio:AccountSid"] = "ACfake0000000000000000000000000",
            ["Communication:Twilio:AuthToken"] = "faketoken",
            ["Communication:Twilio:SmsFromNumber"] = "+15550000000",
            ["Communication:Twilio:WhatsAppFromNumber"] = "+15550000001",
            ["Communication:Sms:Provider"] = "Twilio",
            ["Communication:WhatsApp:Provider"] = "Twilio",
        };

        var provider = BuildServiceProvider(config);

        provider.GetRequiredService<ISmsSender>().ShouldBeOfType<TwilioSmsSender>();
        provider.GetRequiredService<IWhatsAppSender>().ShouldBeOfType<TwilioWhatsAppSender>();
    }

    [Fact]
    public void ProviderMissingCredentials_FallsBackToLogging()
    {
        // Provider named but the connection string absent — must not crash, must fall back.
        var config = new Dictionary<string, string?>(BaseConfig)
        {
            ["Communication:Email:Provider"] = "Acs",
        };

        var provider = BuildServiceProvider(config);

        provider.GetRequiredService<IEmailSender>().ShouldBeOfType<LoggingEmailSender>();
    }

    private static ServiceProvider BuildServiceProvider(IDictionary<string, string?> configValues)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCommunicationInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
