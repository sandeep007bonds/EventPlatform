namespace Communication.Tests.Sending;

public sealed class NotificationRequestValidatorTests
{
    [Fact]
    public void Validate_EmailWithoutTemplateKey_ReturnsError()
    {
        var command = new SendNotificationCommand(Guid.NewGuid(), NotificationChannel.Email, "buyer@example.com", null, null, null, null);

        var errors = NotificationRequestValidator.Validate(command);

        errors.ShouldContain(e => e.Contains("templateKey", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(NotificationChannel.Sms)]
    [InlineData(NotificationChannel.WhatsApp)]
    public void Validate_SmsOrWhatsAppWithoutBody_ReturnsError(NotificationChannel channel)
    {
        var command = new SendNotificationCommand(Guid.NewGuid(), channel, "+15551234567", null, null, null, null);

        var errors = NotificationRequestValidator.Validate(command);

        errors.ShouldContain(e => e.Contains("body", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_MissingRecipient_ReturnsError()
    {
        var command = new SendNotificationCommand(Guid.NewGuid(), NotificationChannel.Sms, string.Empty, null, null, "hi", null);

        var errors = NotificationRequestValidator.Validate(command);

        errors.ShouldContain(e => e.Contains("recipient", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ValidEmailCommand_ReturnsNoErrors()
    {
        var command = new SendNotificationCommand(
            Guid.NewGuid(),
            NotificationChannel.Email,
            "buyer@example.com",
            TemplateKeys.OtpCode,
            new Dictionary<string, string> { ["code"] = "123456" },
            null,
            null);

        var errors = NotificationRequestValidator.Validate(command);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_ValidSmsCommand_ReturnsNoErrors()
    {
        var command = new SendNotificationCommand(Guid.NewGuid(), NotificationChannel.Sms, "+15551234567", null, null, "hi", null);

        var errors = NotificationRequestValidator.Validate(command);

        errors.ShouldBeEmpty();
    }
}
