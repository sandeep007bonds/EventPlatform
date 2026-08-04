namespace Identity.Domain;

/// <summary>
/// An organizer's durable identity — email+password, tenant-scoped (unlike
/// <see cref="BuyerAccount"/>, which is deliberately tenant-less per ADR-0022). Created together
/// with its owning <see cref="Tenant"/> at registration (ADR-0023); one account per tenant this
/// pass — inviting additional organizers into an existing tenant is explicitly deferred.
/// </summary>
public sealed class OrganizerAccount
{
    /// <summary>The lockout threshold — an account that reaches this many consecutive wrong passwords locks out.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>How long an account stays locked out once <see cref="MaxFailedAttempts"/> is reached.</summary>
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private OrganizerAccount()
    {
    }

    private OrganizerAccount(Guid tenantId, string email, string passwordHash)
    {
        Id = Guid.CreateVersion7();
        TenantId = tenantId;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = DateTimeOffset.UtcNow;
        FailedLoginCount = 0;
    }

    /// <summary>The organizer's stable id — minted as the JWT <c>sub</c> claim.</summary>
    public Guid Id { get; private set; }

    /// <summary>The owning tenant's id — also stamped as the JWT <c>tenant_id</c> claim.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The organizer's login email (globally unique).</summary>
    public string Email { get; private set; } = default!;

    /// <summary>The hashed password. Opaque here — never inspected outside the password hasher.</summary>
    public string PasswordHash { get; private set; } = default!;

    /// <summary>When this account was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When this organizer last logged in successfully, if ever.</summary>
    public DateTimeOffset? LastLoginAt { get; private set; }

    /// <summary>Consecutive failed login attempts since the last successful login (or lockout reset).</summary>
    public int FailedLoginCount { get; private set; }

    /// <summary>When the current lockout (if any) expires.</summary>
    public DateTimeOffset? LockedUntil { get; private set; }

    /// <summary>Registers a new organizer account for a newly-created tenant.</summary>
    /// <param name="tenantId">The owning tenant's id.</param>
    /// <param name="email">The login email.</param>
    /// <param name="passwordHash">The already-hashed password.</param>
    /// <returns>A new <see cref="OrganizerAccount"/>.</returns>
    public static OrganizerAccount Register(Guid tenantId, string email, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        return new OrganizerAccount(tenantId, email, passwordHash);
    }

    /// <summary>Whether this account is currently locked out, as of <paramref name="now"/>.</summary>
    /// <param name="now">The current time.</param>
    public bool IsLockedOut(DateTimeOffset now) => LockedUntil is { } lockedUntil && now < lockedUntil;

    /// <summary>Records a failed login attempt, locking the account once <see cref="MaxFailedAttempts"/> is reached.</summary>
    /// <param name="now">The current time.</param>
    /// <returns><see langword="true"/> if this attempt triggered a new lockout.</returns>
    public bool RecordFailedLogin(DateTimeOffset now)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= MaxFailedAttempts)
        {
            LockedUntil = now.Add(LockoutDuration);
            return true;
        }

        return false;
    }

    /// <summary>Records a successful login, clearing any failed-attempt/lockout state.</summary>
    /// <param name="now">The current time.</param>
    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginCount = 0;
        LockedUntil = null;
        LastLoginAt = now;
    }
}
