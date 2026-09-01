namespace Catalog.Application.Abstractions;

/// <summary>
/// Strips anything executable or navigable-away from organizer-authored HTML before it is stored.
/// </summary>
/// <remarks>
/// An abstraction rather than a direct package call so the Application layer keeps no dependency on
/// a sanitiser implementation, and so a handler's test can assert "the sanitised text is what gets
/// persisted" without pulling in an HTML parser.
/// </remarks>
public interface IHtmlSanitizer
{
    /// <summary>Returns a version of <paramref name="html"/> that is safe to render.</summary>
    /// <param name="html">Untrusted HTML, as typed by an organizer.</param>
    /// <returns>The sanitised HTML; may be empty if nothing survived.</returns>
    string Sanitize(string html);
}
