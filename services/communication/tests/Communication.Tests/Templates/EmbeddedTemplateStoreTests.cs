namespace Communication.Tests.Templates;

public sealed class EmbeddedTemplateStoreTests
{
    private readonly EmbeddedTemplateStore store = new();

    [Fact]
    public async Task GetAsync_OtpCode_ReturnsShippedTemplate()
    {
        var template = (await store.GetAsync(TemplateKeys.OtpCode, CancellationToken.None)).ShouldNotBeNull();

        template.Key.ShouldBe(TemplateKeys.OtpCode);
        template.Subject.ShouldNotBeNullOrWhiteSpace();
        template.Body.ShouldContain("{{ code }}");
    }

    [Fact]
    public async Task GetAsync_UnknownKey_ReturnsNull()
    {
        var template = await store.GetAsync("no-such-template", CancellationToken.None);

        template.ShouldBeNull();
    }
}
