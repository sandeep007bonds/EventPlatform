namespace Communication.Tests.Templates;

public sealed class ScribanTemplateRendererTests
{
    private readonly ScribanTemplateRenderer renderer = new();

    [Fact]
    public void Render_SubstitutesKnownPlaceholders()
    {
        var result = renderer.Render(
            "Your one-time code is {{ code }}. It expires in {{ expires_in_minutes }} minutes.",
            new Dictionary<string, string> { ["code"] = "123456", ["expires_in_minutes"] = "10" });

        result.ShouldBe("Your one-time code is 123456. It expires in 10 minutes.");
    }

    [Fact]
    public void Render_MissingPlaceholder_RendersEmpty()
    {
        var result = renderer.Render("Hello {{ name }}!", new Dictionary<string, string>());

        result.ShouldBe("Hello !");
    }

    [Fact]
    public void Render_NoPlaceholders_ReturnsTemplateUnchanged()
    {
        var result = renderer.Render("Plain text, no placeholders.", new Dictionary<string, string>());

        result.ShouldBe("Plain text, no placeholders.");
    }
}
