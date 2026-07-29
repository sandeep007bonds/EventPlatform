namespace Communication.Infrastructure.Templates;

/// <summary>
/// Reads email templates from resource files embedded in this assembly
/// (<c>Templates/Resources/{key}.subject.txt</c> / <c>{key}.body.txt</c>). Templates are shipped
/// with the code that references their keys rather than stored in the database — nothing in this
/// service's requirements needs organizer-editable templates at runtime. A future DB-backed store
/// would be a new adapter behind <see cref="ITemplateStore"/>, not a change to callers.
/// </summary>
internal sealed class EmbeddedTemplateStore : ITemplateStore
{
    private static readonly Assembly ResourceAssembly = typeof(EmbeddedTemplateStore).Assembly;

    /// <inheritdoc />
    public Task<NotificationTemplate?> GetAsync(string templateKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);

        var subject = ReadResource($"{templateKey}.subject.txt");
        var body = ReadResource($"{templateKey}.body.txt");

        if (subject is null || body is null)
        {
            return Task.FromResult<NotificationTemplate?>(null);
        }

        return Task.FromResult<NotificationTemplate?>(new NotificationTemplate(templateKey, subject, body));
    }

    private static string? ReadResource(string fileName)
    {
        var resourceName = $"Communication.Infrastructure.Templates.Resources.{fileName}";

        using var stream = ResourceAssembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Trim();
    }
}
