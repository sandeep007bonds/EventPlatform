namespace Communication.Tests.Senders;

public sealed class LoggingSendersTests
{
    [Fact]
    public async Task LoggingEmailSender_ReturnsSyntheticSuccess()
    {
        var sender = new LoggingEmailSender(NullLogger<LoggingEmailSender>.Instance);

        var result = await sender.SendAsync("buyer@example.com", "Subject", "<p>Body</p>", CancellationToken.None);

        sender.Provider.ShouldBe("dev-log");
        result.Succeeded.ShouldBeTrue();
        result.ProviderReference.ShouldStartWith("log_");
        result.FailureReason.ShouldBeNull();
    }

    [Fact]
    public async Task LoggingSmsSender_ReturnsSyntheticSuccess()
    {
        var sender = new LoggingSmsSender(NullLogger<LoggingSmsSender>.Instance);

        var result = await sender.SendAsync("+15551234567", "Your code is 123456.", CancellationToken.None);

        sender.Provider.ShouldBe("dev-log");
        result.Succeeded.ShouldBeTrue();
        result.ProviderReference.ShouldStartWith("log_");
    }

    [Fact]
    public async Task LoggingWhatsAppSender_ReturnsSyntheticSuccess()
    {
        var sender = new LoggingWhatsAppSender(NullLogger<LoggingWhatsAppSender>.Instance);

        var result = await sender.SendAsync("+15551234567", "Your code is 123456.", CancellationToken.None);

        sender.Provider.ShouldBe("dev-log");
        result.Succeeded.ShouldBeTrue();
        result.ProviderReference.ShouldStartWith("log_");
    }
}
