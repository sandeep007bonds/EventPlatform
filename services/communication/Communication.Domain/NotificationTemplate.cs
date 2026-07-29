namespace Communication.Domain;

/// <summary>A named, renderable email template (subject + body, both placeholder-driven).</summary>
/// <param name="Key">The template's lookup key (see <see cref="TemplateKeys"/>).</param>
/// <param name="Subject">The unrendered subject line template.</param>
/// <param name="Body">The unrendered body template.</param>
public sealed record NotificationTemplate(string Key, string Subject, string Body);
