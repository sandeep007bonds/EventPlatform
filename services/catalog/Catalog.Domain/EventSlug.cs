namespace Catalog.Domain;

/// <summary>
/// Turns an event title into a URL-safe slug, and says which slugs are not allowed.
/// </summary>
/// <remarks>
/// Uniqueness is not decided here — that needs a repository, so the caller supplies a set of slugs
/// already taken and this appends a numeric suffix until one is free. Keeping the string work pure
/// means it is testable without a database.
/// </remarks>
public static class EventSlug
{
    /// <summary>Longest slug we will generate or accept.</summary>
    public const int MaxLength = 120;

    /// <summary>
    /// Slugs that would collide with an application route or read as an official page.
    /// </summary>
    /// <remarks>
    /// The SPA serves <c>/events/{slug}</c>, so a slug is not *strictly* able to shadow a top-level
    /// route today. It is still refused: routes move, and an event legitimately called "admin" or
    /// "checkout" is a phishing surface an organizer should not be handed by accident.
    /// </remarks>
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "api", "auth", "login", "logout", "register", "checkout", "orders", "tickets",
        "events", "event", "new", "edit", "delete", "me", "settings", "health", "static", "assets",
    };

    /// <summary>Whether a slug is well-formed and not reserved.</summary>
    /// <param name="slug">The candidate slug.</param>
    /// <returns><see langword="true"/> if it may be used.</returns>
    public static bool IsValid(string? slug) =>
        !string.IsNullOrWhiteSpace(slug)
        && slug.Length <= MaxLength
        && !Reserved.Contains(slug)
        && slug.All(c => (c is >= 'a' and <= 'z') || (c is >= '0' and <= '9') || c == '-')
        && !slug.StartsWith('-')
        && !slug.EndsWith('-')
        && !slug.Contains("--", StringComparison.Ordinal);

    /// <summary>
    /// The stem a title slugifies to, before any uniqueness suffix.
    /// </summary>
    /// <remarks>
    /// Public because uniqueness needs a database round-trip and the caller has to know what to
    /// query for: it fetches the slugs equal to this stem or beginning <c>{stem}-</c>, then passes
    /// them to <see cref="From"/>. Fetching every slug on the platform to build that set would
    /// work exactly once.
    /// </remarks>
    /// <param name="title">The event title.</param>
    /// <returns>A valid, non-empty, non-reserved stem.</returns>
    public static string Basis(string title)
    {
        var basis = Slugify(title);

        // A title of only punctuation, or one that slugifies to a reserved word. "event" is
        // reserved precisely so this cannot silently produce it, hence the different stem.
        return basis.Length == 0 || Reserved.Contains(basis) ? "e" : basis;
    }

    /// <summary>
    /// Derives a slug from a title, appending a numeric suffix while the result is already taken.
    /// </summary>
    /// <param name="title">The event title.</param>
    /// <param name="taken">Slugs already in use; compared case-insensitively.</param>
    /// <returns>A valid, unused slug.</returns>
    public static string From(string title, IReadOnlySet<string> taken)
    {
        ArgumentNullException.ThrowIfNull(taken);

        var basis = Basis(title);

        if (!taken.Contains(basis))
        {
            return basis;
        }

        // Bounded rather than while(true): a caller passing a set containing every candidate would
        // otherwise spin forever. Two thousand identically-titled events is not a real scenario, so
        // falling back to a random suffix past that is a safety valve, not a design.
        for (var suffix = 2; suffix < 2000; suffix++)
        {
            var candidate = Truncate(basis, MaxLength - 5) + "-" + suffix.ToString(CultureInfo.InvariantCulture);
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }

        return Truncate(basis, MaxLength - 9) + "-" + Guid.CreateVersion7().ToString("N")[..8];
    }

    private static string Slugify(string title)
    {
        var builder = new StringBuilder(title.Length);
        var lastWasHyphen = true;

        foreach (var ch in title.Trim().ToLowerInvariant())
        {
            if ((ch is >= 'a' and <= 'z') || (ch is >= '0' and <= '9'))
            {
                builder.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                // Any run of non-alphanumerics collapses to one hyphen, and a leading run produces
                // none — so "  Coldplay: Music of the Spheres!  " gives a clean slug rather than
                // one bracketed by separators.
                builder.Append('-');
                lastWasHyphen = true;
            }

            if (builder.Length >= MaxLength)
            {
                break;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength].TrimEnd('-');
}
