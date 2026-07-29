namespace Communication.Application.Abstractions;

/// <summary>Renders a template string against a set of named placeholders.</summary>
public interface ITemplateRenderer
{
    /// <summary>Renders <paramref name="template"/>, substituting each <c>{{ key }}</c> with its placeholder value.</summary>
    /// <param name="template">The unrendered template text.</param>
    /// <param name="placeholders">The placeholder values, keyed by name.</param>
    /// <returns>The rendered text.</returns>
    string Render(string template, IReadOnlyDictionary<string, string> placeholders);
}
