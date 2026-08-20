namespace Media.Api.Endpoints;

/// <summary>Maps the Media HTTP endpoints — a single image-upload endpoint, no download proxy.</summary>
public static class MediaEndpoints
{
    /// <summary>
    /// The blob container images are uploaded to. Public-read (<see cref="PublicAccessType.Blob"/>)
    /// — the browser fetches an uploaded image's URL directly from storage, no proxy endpoint.
    /// </summary>
    public const string ContainerName = "event-media";

    private const long MaxFileSizeBytes = 8 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif",
    };

    private static readonly Dictionary<string, string> ExtensionsByContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    /// <summary>Maps the <c>/v1/media</c> endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/v1/media").WithTags("Media");

        // Organizer-only: uploads cost storage and appear on a public event page, so an anonymous
        // or buyer caller has no business writing here.
        group.MapPost("/images", UploadImageAsync).WithName("UploadImage").RequireOrganizer().DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> UploadImageAsync(
        IFormFile file,
        ITenantContext tenant,
        BlobServiceClient blobServiceClient,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        if (file.Length == 0)
        {
            return Results.BadRequest(new { message = "No file was uploaded." });
        }

        if (file.Length > MaxFileSizeBytes)
        {
            return Results.BadRequest(new { message = $"The file exceeds the {MaxFileSizeBytes / (1024 * 1024)} MB limit." });
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return Results.BadRequest(new { message = $"Unsupported content type '{file.ContentType}'. Allowed: {string.Join(", ", AllowedContentTypes)}." });
        }

        var extension = ExtensionsByContentType[file.ContentType];
        var blobName = $"tenants/{tenant.TenantId}/{Guid.CreateVersion7()}{extension}";

        var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(
            stream,
            new BlobHttpHeaders { ContentType = file.ContentType },
            cancellationToken: cancellationToken);

        return Results.Created(blobClient.Uri.ToString(), new { url = blobClient.Uri.ToString() });
    }
}
