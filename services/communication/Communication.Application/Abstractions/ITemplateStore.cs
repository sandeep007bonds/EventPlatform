namespace Communication.Application.Abstractions;

/// <summary>Looks up a named email template. Implemented in Infrastructure by an embedded-resource-backed store.</summary>
public interface ITemplateStore
{
    /// <summary>Gets a template by its key.</summary>
    /// <param name="templateKey">The template key (see <see cref="TemplateKeys"/>).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The template, or <see langword="null"/> if no template exists for that key.</returns>
    Task<NotificationTemplate?> GetAsync(string templateKey, CancellationToken cancellationToken);
}
