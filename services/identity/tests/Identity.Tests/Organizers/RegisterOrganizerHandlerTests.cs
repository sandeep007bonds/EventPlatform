namespace Identity.Tests.Organizers;

public sealed class RegisterOrganizerHandlerTests
{
    private readonly IOrganizerRepository repository = Substitute.For<IOrganizerRepository>();
    private readonly IPasswordHasher hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenIssuer tokenIssuer = Substitute.For<ITokenIssuer>();
    private readonly RegisterOrganizerHandler handler;

    public RegisterOrganizerHandlerTests()
    {
        hasher.Hash(Arg.Any<string>()).Returns("hashed-password");
        handler = new RegisterOrganizerHandler(repository, hasher, tokenIssuer);
    }

    [Fact]
    public async Task HandleAsync_InvalidCommand_ReturnsValidationFailed_WithoutTouchingRepository()
    {
        var result = await handler.HandleAsync(
            new RegisterOrganizerCommand(string.Empty, "not-an-email", "short"),
            CancellationToken.None);

        result.Outcome.ShouldBe(RegisterOrganizerOutcome.ValidationFailed);
        await repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmailAlreadyRegistered_ReturnsEmailAlreadyRegistered()
    {
        repository.GetOrganizerByEmailAsync("organizer@example.com", Arg.Any<CancellationToken>())
            .Returns(OrganizerAccount.Register(Guid.NewGuid(), "organizer@example.com", "existing-hash"));

        var result = await handler.HandleAsync(
            new RegisterOrganizerCommand("Acme Events", "organizer@example.com", "password123"),
            CancellationToken.None);

        result.Outcome.ShouldBe(RegisterOrganizerOutcome.EmailAlreadyRegistered);
        repository.DidNotReceive().AddTenant(Arg.Any<Tenant>());
    }

    [Fact]
    public async Task HandleAsync_NewEmail_CreatesTenantAndAccount_AndIssuesAnOrganizerToken()
    {
        repository.GetOrganizerByEmailAsync("organizer@example.com", Arg.Any<CancellationToken>())
            .Returns((OrganizerAccount?)null);

        var issuedToken = new IssuedAccessToken("token", DateTimeOffset.UtcNow.AddDays(7));
        tokenIssuer.IssueAsync(Arg.Any<Guid>(), "organizer", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(issuedToken);

        var result = await handler.HandleAsync(
            new RegisterOrganizerCommand("Acme Events", "organizer@example.com", "password123"),
            CancellationToken.None);

        result.Outcome.ShouldBe(RegisterOrganizerOutcome.Registered);
        result.Token.ShouldBe(issuedToken);
        result.OrganizerId.ShouldNotBeNull();
        result.TenantId.ShouldNotBeNull();

        repository.Received(1).AddTenant(Arg.Is<Tenant>(t => t.Name == "Acme Events"));
        repository.Received(1).AddOrganizerAccount(Arg.Is<OrganizerAccount>(a =>
            a.Email == "organizer@example.com" && a.TenantId == result.TenantId));
        await tokenIssuer.Received(1).IssueAsync(result.OrganizerId!.Value, "organizer", result.TenantId, Arg.Any<CancellationToken>());
    }
}
