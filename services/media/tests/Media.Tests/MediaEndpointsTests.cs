namespace Media.Tests;

/// <summary>
/// Integration tests against a real Azurite container, proving the upload endpoint actually
/// writes a fetchable blob — and rejects what it should reject — end to end over HTTP.
/// </summary>
public sealed class MediaEndpointsTests : IAsyncLifetime
{
    private const string DevSigningKey = "eventplatform-dev-hs256-signing-key-not-a-secret";
    private const string Issuer = "eventplatform-dev";
    private const string Audience = "eventplatform";
    private const string OrganizerRole = "organizer";
    private const string BuyerRole = "buyer";

    // --skipApiVersionCheck for the same reason docker-compose.yml passes it: the pinned
    // Azure.Storage.Blobs SDK negotiates a newer x-ms-version than the Azurite image recognizes
    // ("API version ... is not supported by Azurite"), because Azurite trails the newest Azure REST
    // API releases. Without it every test here fails at container-create before touching the
    // endpoint under test. Emulator-only — real Azure Blob Storage has no such check.
    private readonly AzuriteContainer azurite = new AzuriteBuilder()
        .WithCommand("--skipApiVersionCheck")
        .Build();

    private WebApplicationFactory<Program> factory = default!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await azurite.StartAsync();

        // These MUST be environment variables, not ConfigureAppConfiguration entries.
        //
        // Media.Api's Program.cs calls AddServiceDefaults() — which reads Jwt:DevSigningKey and
        // wires up JwtBearer — *before* builder.Build(). WebApplicationFactory only applies its
        // ConfigureAppConfiguration sources during Build(), so anything added there arrives too
        // late to influence authentication. (The Azurite connection string below works precisely
        // because it is read later, from inside a DI factory lambda, once the container is built.)
        //
        // WebApplication.CreateBuilder() adds AddEnvironmentVariables() at construction, so values
        // set here are visible at that eager read. `__` maps to `:`, the same convention
        // scripts/dev-up.sh uses for Payments__Stripe__WebhookSecret.
        //
        // Without this the service takes its production OIDC branch — appsettings.Development.json
        // points Jwt:Authority at the Identity service and sets no DevSigningKey since ADR-0023 —
        // attempts discovery against a service that is not running here, and 401s every request.
        Environment.SetEnvironmentVariable("Jwt__DevSigningKey", DevSigningKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", Issuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", Audience);

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:media-storage"] = azurite.GetConnectionString(),
                });
            });
        });
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await factory.DisposeAsync();
        await azurite.DisposeAsync();

        Environment.SetEnvironmentVariable("Jwt__DevSigningKey", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
    }

    [Fact]
    public async Task UploadImage_ValidPngWithinSizeLimit_ReturnsFetchableUrl()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MintToken());

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(BuildFakePngBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "banner.png");

        var response = await client.PostAsync("/v1/media/images", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UploadResponse>();
        body.ShouldNotBeNull();
        body.Url.ShouldNotBeNullOrWhiteSpace();

        using var downloadClient = new HttpClient();
        var downloaded = await downloadClient.GetAsync(body.Url);
        downloaded.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UploadImage_UnsupportedContentType_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", MintToken());

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "not-an-image.pdf");

        var response = await client.PostAsync("/v1/media/images", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // The test that would have caught the 403 this file's MintToken comment describes. The 201 case
    // above fails on any misconfiguration at all; only a wrong-role case proves the policy
    // discriminates rather than merely denying everything or allowing everything.
    [Fact]
    public async Task UploadImage_BuyerRoleToken_ReturnsForbidden()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", MintToken(BuyerRole));

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(BuildFakePngBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "banner.png");

        var response = await client.PostAsync("/v1/media/images", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadImage_NoBearerToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(BuildFakePngBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "banner.png");

        var response = await client.PostAsync("/v1/media/images", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // Matches DevTokenIssuer's claim shape (see gateways/EventPlatform.Gateway/DevAuth) —
    // Media.Api validates against the same DevSigningKey path every service uses in Development.
    //
    // The `role` claim is the part that matters here and it was missing until the 403 that led to
    // this fix: the upload endpoint is RequireOrganizer(), so a token without a role could never
    // have produced the 201 the first test asserts. Note the short claim name, which reaches the
    // policy intact only because AuthenticationExtensions sets MapInboundClaims = false.
    private static string MintToken(string role = OrganizerRole)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new("tenant_id", Guid.NewGuid().ToString()),
            new("role", role),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DevSigningKey)),
            SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(Issuer, Audience, claims, now, now.AddHours(1), credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // A minimal-but-valid 1x1 PNG — enough to exercise real bytes through the upload path
    // without needing an image library dependency just for a test fixture.
    private static byte[] BuildFakePngBytes() =>
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
        0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41,
        0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00,
        0x00, 0x03, 0x01, 0x01, 0x00, 0x18, 0xDD, 0x8D,
        0xB0, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
        0x44, 0xAE, 0x42, 0x60, 0x82,
    ];

    private sealed record UploadResponse(string Url);
}
