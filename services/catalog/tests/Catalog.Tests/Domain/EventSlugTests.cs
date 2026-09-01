namespace Catalog.Tests.Domain;

// The slug ends up in a URL people share and a unique index the database enforces, so the two
// things worth pinning are that it only ever emits characters a URL tolerates, and that it never
// returns a value the caller already told it was taken.
public sealed class EventSlugTests
{
    [Theory]
    [InlineData("ColdPlay India Tour — Mumbai", "coldplay-india-tour-mumbai")]
    [InlineData("  Coldplay: Music of the Spheres!  ", "coldplay-music-of-the-spheres")]
    [InlineData("AC/DC @ Wembley", "ac-dc-wembley")]
    [InlineData("2027", "2027")]
    public void ATitle_SlugifiesToUrlSafeText(string title, string expected) =>
        EventSlug.From(title, new HashSet<string>()).ShouldBe(expected);

    // "!!!" has nothing to slugify and "admin" would shadow a route; both fall back to the same
    // stem rather than producing an empty or reserved slug.
    [Theory]
    [InlineData("!!!")]
    [InlineData("admin")]
    [InlineData("Checkout")]
    public void ATitleThatCannotBecomeASlug_FallsBackToASafeStem(string title) =>
        EventSlug.From(title, new HashSet<string>()).ShouldBe("e");

    [Fact]
    public void ATakenSlug_GetsANumericSuffix()
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "coldplay-mumbai" };

        EventSlug.From("Coldplay Mumbai", taken).ShouldBe("coldplay-mumbai-2");
    }

    [Fact]
    public void SuffixesKeepClimbingUntilOneIsFree()
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "coldplay-mumbai", "coldplay-mumbai-2", "coldplay-mumbai-3",
        };

        EventSlug.From("Coldplay Mumbai", taken).ShouldBe("coldplay-mumbai-4");
    }

    // Case matters here: the taken set comes from Postgres, and "Coldplay-Mumbai" and
    // "coldplay-mumbai" would collide on the unique index even though the strings differ.
    [Fact]
    public void TheTakenSetIsHonouredCaseInsensitively()
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Coldplay-Mumbai" };

        EventSlug.From("Coldplay Mumbai", taken).ShouldBe("coldplay-mumbai-2");
    }

    [Fact]
    public void AVeryLongTitle_IsTruncatedWithinTheColumnLength()
    {
        var slug = EventSlug.From(new string('a', 400), new HashSet<string>());

        slug.Length.ShouldBeLessThanOrEqualTo(EventSlug.MaxLength);
    }

    [Theory]
    [InlineData("coldplay-mumbai", true)]
    [InlineData("2027", true)]
    [InlineData("Coldplay-Mumbai", false)]
    [InlineData("coldplay mumbai", false)]
    [InlineData("-coldplay", false)]
    [InlineData("coldplay-", false)]
    [InlineData("coldplay--mumbai", false)]
    [InlineData("admin", false)]
    [InlineData("", false)]
    public void IsValid_AcceptsOnlyWellFormedUnreservedSlugs(string slug, bool expected) =>
        EventSlug.IsValid(slug).ShouldBe(expected);
}
