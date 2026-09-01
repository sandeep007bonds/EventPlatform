namespace Catalog.Infrastructure;

/// <summary>
/// <see cref="IHtmlSanitizer"/> over Ganss.Xss, narrowed to what a legal document actually needs.
/// </summary>
/// <remarks>
/// The default allow-list is aimed at general rich text and permits rather more than a terms page
/// should carry — images, iframes, styles. This trims it to structure and emphasis: nothing here
/// can load a remote resource, so a policy page cannot be turned into a tracking beacon that fires
/// on every buyer who opens it before checkout.
/// <para>
/// Links survive, because a refund policy that cannot link to a contact page is not much of a
/// policy — but only <c>http</c>, <c>https</c> and <c>mailto</c> schemes, which is what excludes
/// <c>javascript:</c>.
/// </para>
/// </remarks>
internal sealed class PolicyHtmlSanitizer : IHtmlSanitizer
{
    private readonly GanssHtmlSanitizer sanitizer = BuildSanitizer();

    /// <inheritdoc />
    public string Sanitize(string html) => sanitizer.Sanitize(html ?? string.Empty);

    private static GanssHtmlSanitizer BuildSanitizer()
    {
        var built = new GanssHtmlSanitizer();

        built.AllowedTags.Clear();
        foreach (var tag in new[]
        {
            "p", "br", "hr", "strong", "b", "em", "i", "u", "s", "sub", "sup",
            "h1", "h2", "h3", "h4", "h5", "h6",
            "ul", "ol", "li", "blockquote", "pre", "code",
            "table", "thead", "tbody", "tr", "th", "td",
            "a", "span", "div",
        })
        {
            built.AllowedTags.Add(tag);
        }

        built.AllowedAttributes.Clear();
        built.AllowedAttributes.Add("href");
        built.AllowedAttributes.Add("title");
        built.AllowedAttributes.Add("colspan");
        built.AllowedAttributes.Add("rowspan");

        built.AllowedSchemes.Clear();
        built.AllowedSchemes.Add("http");
        built.AllowedSchemes.Add("https");
        built.AllowedSchemes.Add("mailto");

        built.AllowedCssProperties.Clear();
        built.AllowDataAttributes = false;

        return built;
    }
}
