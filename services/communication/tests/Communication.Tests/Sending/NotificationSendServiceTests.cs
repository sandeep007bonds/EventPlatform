namespace Communication.Tests.Sending;

public sealed class NotificationSendServiceTests
{
    private readonly IEmailSender emailSender = Substitute.For<IEmailSender>();
    private readonly ISmsSender smsSender = Substitute.For<ISmsSender>();
    private readonly IWhatsAppSender whatsAppSender = Substitute.For<IWhatsAppSender>();
    private readonly ITemplateStore templates = Substitute.For<ITemplateStore>();
    private readonly ITemplateRenderer renderer = Substitute.For<ITemplateRenderer>();
    private readonly INotificationRepository notifications = Substitute.For<INotificationRepository>();

    [Fact]
    public async Task SendAsync_Email_RendersTemplateAndSends()
    {
        var template = new NotificationTemplate(TemplateKeys.OtpCode, "Subject {{ code }}", "Body {{ code }}");
        templates.GetAsync(TemplateKeys.OtpCode, Arg.Any<CancellationToken>()).Returns(template);
        renderer.Render("Subject {{ code }}", Arg.Any<IReadOnlyDictionary<string, string>>()).Returns("Subject 123456");
        renderer.Render("Body {{ code }}", Arg.Any<IReadOnlyDictionary<string, string>>()).Returns("Body 123456");
        emailSender.Provider.Returns("dev-log");
        emailSender.SendAsync("buyer@example.com", "Subject 123456", "Body 123456", Arg.Any<CancellationToken>())
            .Returns(new SendResult(true, "log_abc", null));

        var command = new SendNotificationCommand(
            Guid.NewGuid(),
            NotificationChannel.Email,
            "buyer@example.com",
            TemplateKeys.OtpCode,
            new Dictionary<string, string> { ["code"] = "123456" },
            null,
            null);

        var result = await CreateService().SendAsync(command, CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        result.Provider.ShouldBe("dev-log");
        await emailSender.Received(1).SendAsync("buyer@example.com", "Subject 123456", "Body 123456", Arg.Any<CancellationToken>());
        notifications.Received(1).AddDeliveryLog(Arg.Is<DeliveryLogEntry>(e => e.Status == DeliveryStatus.Sent));
        await notifications.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_Email_UnknownTemplate_Throws()
    {
        templates.GetAsync("no-such-template", Arg.Any<CancellationToken>()).Returns((NotificationTemplate?)null);

        var command = new SendNotificationCommand(Guid.NewGuid(), NotificationChannel.Email, "buyer@example.com", "no-such-template", null, null, null);

        await Should.ThrowAsync<InvalidOperationException>(() => CreateService().SendAsync(command, CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_Sms_CallsSmsSenderDirectly()
    {
        smsSender.Provider.Returns("dev-log");
        smsSender.SendAsync("+15551234567", "hi", Arg.Any<CancellationToken>()).Returns(new SendResult(true, "log_abc", null));

        var command = new SendNotificationCommand(Guid.NewGuid(), NotificationChannel.Sms, "+15551234567", null, null, "hi", null);

        var result = await CreateService().SendAsync(command, CancellationToken.None);

        result.Succeeded.ShouldBeTrue();
        await templates.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_Failure_RecordsFailedDeliveryLog()
    {
        smsSender.Provider.Returns("twilio");
        smsSender.SendAsync("+15551234567", "hi", Arg.Any<CancellationToken>())
            .Returns(new SendResult(false, null, "vendor rejected"));

        var command = new SendNotificationCommand(Guid.NewGuid(), NotificationChannel.Sms, "+15551234567", null, null, "hi", null);

        var result = await CreateService().SendAsync(command, CancellationToken.None);

        result.Succeeded.ShouldBeFalse();
        result.FailureReason.ShouldBe("vendor rejected");
        notifications.Received(1).AddDeliveryLog(Arg.Is<DeliveryLogEntry>(e => e.Status == DeliveryStatus.Failed));
    }

    // A real CorrelationContext rather than a substitute: it self-seeds an id, which is the
    // behaviour under test here as much as anywhere — a delivery-log row must never be written with
    // an empty chain, including from a background send with no request behind it.
    private NotificationSendService CreateService() =>
        new(emailSender, smsSender, whatsAppSender, templates, renderer, notifications, new CorrelationContext());
}
