namespace Catalog.Domain;

/// <summary>
/// One legal document — terms, privacy notice or refund policy — as HTML, either an organizer's
/// tenant-wide default or an override for a single event.
/// </summary>
/// <remarks>
/// <para>
/// <b>Defaults and overrides.</b> A document with no <see cref="EventId"/> is the tenant's default
/// for that <see cref="Kind"/>; one with an <see cref="EventId"/> replaces it for that event only.
/// This is the same shape <see cref="EventGroup"/> already uses for tour contact details, so the
/// resolution rule an organizer has to hold in their head stays the same across the product.
/// </para>
/// <para>
/// <b>Versioned, because a refund dispute is a question about the past.</b> <see cref="Version"/>
/// increments on every revision and Ordering captures the version in force at checkout. Without
/// that, "what did the buyer agree to" can only be answered with whatever the text says today,
/// which is precisely the wrong answer whenever it matters.
/// </para>
/// <para>
/// <b>Sanitising is the caller's job, and is not optional.</b> This aggregate stores whatever HTML
/// it is handed; the application layer strips scripts and event handlers before calling. Doing it
/// on write rather than on render means a stored payload cannot be reached by some future reader
/// that forgets to escape — but it also means an unsanitised write is a persistent XSS, so the
/// handler is the only supported way in.
/// </para>
/// </remarks>
public sealed class PolicyDocument
{
    // Parameterless ctor for EF Core materialization.
    private PolicyDocument()
    {
    }

    private PolicyDocument(Guid id, Guid tenantId, Guid? eventId, PolicyKind kind, string bodyHtml, DateTimeOffset updatedAt)
    {
        Id = id;
        TenantId = tenantId;
        EventId = eventId;
        Kind = kind;
        BodyHtml = bodyHtml;
        Version = 1;
        UpdatedAt = updatedAt;
    }

    /// <summary>Unique document id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant (organizer).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// The event this document overrides the tenant default for, or <see langword="null"/> when it
    /// <em>is</em> the tenant default.
    /// </summary>
    public Guid? EventId { get; private set; }

    /// <summary>Which document this is.</summary>
    public PolicyKind Kind { get; private set; }

    /// <summary>The document body, as sanitised HTML.</summary>
    public string BodyHtml { get; private set; } = default!;

    /// <summary>
    /// Revision number, starting at 1 and incrementing on every <see cref="Revise"/>. Captured on
    /// an order at checkout so the terms a buyer accepted can be identified later.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>When this document was last revised (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Creates the first version of a policy document.</summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="eventId">The event this overrides, or <see langword="null"/> for the tenant default.</param>
    /// <param name="kind">Which document this is.</param>
    /// <param name="bodyHtml">The already-sanitised body HTML.</param>
    /// <param name="now">The current time (UTC).</param>
    /// <returns>A new <see cref="PolicyDocument"/> at version 1.</returns>
    /// <exception cref="ArgumentException"><paramref name="bodyHtml"/> is null or blank.</exception>
    public static PolicyDocument Create(Guid tenantId, Guid? eventId, PolicyKind kind, string bodyHtml, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bodyHtml);

        return new PolicyDocument(Guid.CreateVersion7(), tenantId, eventId, kind, bodyHtml, now);
    }

    /// <summary>Replaces the body with a new revision, incrementing <see cref="Version"/>.</summary>
    /// <param name="bodyHtml">The already-sanitised body HTML.</param>
    /// <param name="now">The current time (UTC).</param>
    /// <exception cref="ArgumentException"><paramref name="bodyHtml"/> is null or blank.</exception>
    public void Revise(string bodyHtml, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bodyHtml);

        if (bodyHtml == BodyHtml)
        {
            // No-op rather than a new version. An organizer opening the editor, changing nothing and
            // pressing Save must not invalidate the version every existing order points at.
            return;
        }

        BodyHtml = bodyHtml;
        Version++;
        UpdatedAt = now;
    }
}
