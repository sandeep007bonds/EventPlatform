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

    private readonly AzuriteContainer azurite = new AzuriteBuilder().Build();
    private WebApplicationFactory<Program> factory = default!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await azurite.StartAsync();

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
        factory.Dispose();
        await azurite.DisposeAsync();
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

    // Matches DevTokenIssuer's claim shape exactly (see gateways/EventPlatform.Gateway/DevAuth) —
    // Media.Api validates against the same DevSigningKey path every service uses in Development.
    private static string MintToken()
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new("tenant_id", Guid.NewGuid().ToString()),
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
