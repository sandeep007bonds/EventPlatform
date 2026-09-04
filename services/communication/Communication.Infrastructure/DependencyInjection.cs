namespace Communication.Infrastructure;

/// <summary>Registers the Communication infrastructure layer with the DI container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the Communication EF Core context (PostgreSQL), repository, template store/renderer,
    /// recipient resolver, and the Email/SMS/WhatsApp senders — each vendor-selected per channel by
    /// configuration, falling back to a dev/logging sender when nothing is configured. The
    /// connection string is read from the <c>communication</c> connection string. Unlike every other
    /// service, this does NOT call <c>AddOutbox</c> — Communication never publishes an integration
    /// event (see <see cref="CommunicationDbContext"/>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddCommunicationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("communication");

        services.AddDbContext<CommunicationDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .UseAuditFields(sp));

        // Subscribes without publishing, so it needs the dead-letter half of the messaging
        // building block but not the outbox half.
        services.AddDeadLetters<CommunicationDbContext>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddSingleton<ITemplateStore, EmbeddedTemplateStore>();
        services.AddSingleton<ITemplateRenderer, ScribanTemplateRenderer>();
        services.AddSingleton<IRecipientResolver, UnavailableRecipientResolver>();

        AddEmailSender(services, configuration);
        AddSmsSender(services, configuration);
        AddWhatsAppSender(services, configuration);

        return services;
    }

    // Email is ACS-only under this plan (Twilio's email product is SendGrid, a different SDK/package
    // — out of scope). Absence of Communication:Email:Provider=Acs (or the connection string) falls
    // back to the dev/logging sender, never crashes.
    private static void AddEmailSender(IServiceCollection services, IConfiguration configuration)
    {
        var acsConnectionString = configuration["Communication:Acs:ConnectionString"];
        var emailProvider = configuration["Communication:Email:Provider"];

        if (string.Equals(emailProvider, "Acs", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(acsConnectionString))
        {
            var fromAddress = configuration["Communication:Acs:EmailFromAddress"]!;
            services.AddSingleton<IEmailSender>(new AcsEmailSender(acsConnectionString, fromAddress));
        }
        else
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }
    }

    private static void AddSmsSender(IServiceCollection services, IConfiguration configuration)
    {
        var acsConnectionString = configuration["Communication:Acs:ConnectionString"];
        var smsProvider = configuration["Communication:Sms:Provider"];

        if (string.Equals(smsProvider, "Acs", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(acsConnectionString))
        {
            var fromNumber = configuration["Communication:Acs:SmsFromNumber"]!;
            services.AddSingleton<ISmsSender>(new AcsSmsSender(acsConnectionString, fromNumber));
        }
        else if (string.Equals(smsProvider, "Twilio", StringComparison.OrdinalIgnoreCase))
        {
            var accountSid = configuration["Communication:Twilio:AccountSid"]!;
            var authToken = configuration["Communication:Twilio:AuthToken"]!;
            var fromNumber = configuration["Communication:Twilio:SmsFromNumber"]!;

            // Twilio's SDK doesn't integrate with IHttpClientFactory on its own — build the sender
            // from an injected HttpClient instead of the SDK's process-wide TwilioClient.Init(...).
            // AddHttpClient<TClient>(Action<HttpClient>) doesn't fit here: TwilioSmsSender's
            // constructor needs accountSid/authToken/fromNumber too, which DI can't resolve on its
            // own — AddTypedClient's Func<HttpClient, TClient> factory overload is what supports that.
            services.AddHttpClient(nameof(TwilioSmsSender))
                .AddTypedClient<TwilioSmsSender>(httpClient => new TwilioSmsSender(httpClient, accountSid, authToken, fromNumber));
            services.AddTransient<ISmsSender>(sp => sp.GetRequiredService<TwilioSmsSender>());
        }
        else
        {
            services.AddSingleton<ISmsSender, LoggingSmsSender>();
        }
    }

    private static void AddWhatsAppSender(IServiceCollection services, IConfiguration configuration)
    {
        var acsConnectionString = configuration["Communication:Acs:ConnectionString"];
        var whatsAppProvider = configuration["Communication:WhatsApp:Provider"];

        if (string.Equals(whatsAppProvider, "Acs", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(acsConnectionString))
        {
            var channelRegistrationId = configuration["Communication:Acs:WhatsAppChannelId"]!;
            services.AddSingleton<IWhatsAppSender>(new AcsWhatsAppSender(acsConnectionString, channelRegistrationId));
        }
        else if (string.Equals(whatsAppProvider, "Twilio", StringComparison.OrdinalIgnoreCase))
        {
            var accountSid = configuration["Communication:Twilio:AccountSid"]!;
            var authToken = configuration["Communication:Twilio:AuthToken"]!;
            var fromNumber = configuration["Communication:Twilio:WhatsAppFromNumber"]!;

            services.AddHttpClient(nameof(TwilioWhatsAppSender))
                .AddTypedClient<TwilioWhatsAppSender>(httpClient => new TwilioWhatsAppSender(httpClient, accountSid, authToken, fromNumber));
            services.AddTransient<IWhatsAppSender>(sp => sp.GetRequiredService<TwilioWhatsAppSender>());
        }
        else
        {
            services.AddSingleton<IWhatsAppSender, LoggingWhatsAppSender>();
        }
    }
}
