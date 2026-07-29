namespace Communication.Infrastructure.Templates;

/// <summary>
/// Renders templates with Scriban rather than hand-rolled string substitution — Scriban correctly
/// handles a missing placeholder and HTML-escapes values by default (relevant since a placeholder
/// could carry user-supplied text, e.g. an event title), and leaves room to grow (loops/conditionals)
/// without a rewrite. Small, dependency-light, BSD-2-Clause licensed.
/// </summary>
internal sealed class ScribanTemplateRenderer : ITemplateRenderer
{
    /// <inheritdoc />
    public string Render(string template, IReadOnlyDictionary<string, string> placeholders)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentNullException.ThrowIfNull(placeholders);

        var parsed = Template.Parse(template);
        if (parsed.HasErrors)
        {
            throw new InvalidOperationException($"Template failed to parse: {string.Join("; ", parsed.Messages)}");
        }

        var scriptObject = new ScriptObject();
        foreach (var (key, value) in placeholders)
        {
            scriptObject[key] = value;
        }

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        return parsed.Render(context);
    }
}
