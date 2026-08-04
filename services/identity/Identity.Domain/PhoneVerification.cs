namespace Identity.Domain;

/// <summary>
/// One OTP challenge issued to a phone number. Never stores the raw code — only a keyed-HMAC
/// hash (see <c>Identity.Infrastructure</c>'s <c>IOtpHasher</c>) — the same "never store the
/// secret plaintext" principle as a password, sized to a 6-digit/5-minute/5-attempt code instead
/// of a slow password KDF (see the ADR-0016 extension for the full justification).
/// </summary>
public sealed class PhoneVerification
{
    /// <summary>The lockout threshold — a challenge that reaches this many wrong guesses can never verify.</summary>
    public const int MaxAttempts = 5;

    private PhoneVerification()
    {
    }

    private PhoneVerification(string phoneNumber, string otpHash, string salt, DateTimeOffset expiresAt)
    {
        Id = Guid.CreateVersion7();
        PhoneNumber = phoneNumber;
        OtpHash = otpHash;
        Salt = salt;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = expiresAt;
        AttemptCount = 0;
        Status = PhoneVerificationStatus.Pending;
    }

    /// <summary>Unique challenge id (UUID v7 — time-sortable).</summary>
    public Guid Id { get; private set; }

    /// <summary>The phone number this challenge was issued to, in E.164 format.</summary>
    public string PhoneNumber { get; private set; } = default!;

    /// <summary>The keyed-HMAC hash of the OTP code — the raw code is never persisted.</summary>
    public string OtpHash { get; private set; } = default!;

    /// <summary>Random per-challenge salt mixed into the HMAC input.</summary>
    public string Salt { get; private set; } = default!;

    /// <summary>When this challenge was issued (UTC) — also the basis for the resend cooldown.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When this challenge's code stops being acceptable (UTC).</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>How many wrong-code attempts have been recorded.</summary>
    public int AttemptCount { get; private set; }

    /// <summary>The current lifecycle state.</summary>
    public PhoneVerificationStatus Status { get; private set; }

    /// <summary>When this challenge was successfully verified, if it was.</summary>
    public DateTimeOffset? VerifiedAt { get; private set; }

    /// <summary>Issues a new pending challenge.</summary>
    /// <param name="phoneNumber">The E.164 phone number the code was sent to.</param>
    /// <param name="otpHash">The keyed-HMAC hash of the generated code.</param>
    /// <param name="salt">The per-challenge salt used in the hash.</param>
    /// <param name="ttl">How long the code stays valid.</param>
    /// <returns>A new <see cref="PhoneVerification"/> in <see cref="PhoneVerificationStatus.Pending"/>.</returns>
    public static PhoneVerification Issue(string phoneNumber, string otpHash, string salt, TimeSpan ttl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(otpHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(salt);

        return new PhoneVerification(phoneNumber, otpHash, salt, DateTimeOffset.UtcNow.Add(ttl));
    }

    /// <summary>Whether the code is still within its TTL as of <paramref name="now"/>.</summary>
    /// <param name="now">The current time.</param>
    /// <returns><see langword="true"/> if <paramref name="now"/> is past <see cref="ExpiresAt"/>.</returns>
    public bool HasExpired(DateTimeOffset now) => now >= ExpiresAt;

    /// <summary>Lazily transitions a pending-but-expired challenge to <see cref="PhoneVerificationStatus.Expired"/>.</summary>
    /// <param name="now">The current time.</param>
    /// <exception cref="InvalidOperationException">The challenge is not <see cref="PhoneVerificationStatus.Pending"/>, or is not actually expired.</exception>
    public void MarkExpired(DateTimeOffset now)
    {
        if (Status != PhoneVerificationStatus.Pending || !HasExpired(now))
        {
            throw new InvalidOperationException("Only a pending, expired challenge can be marked expired.");
        }

        Status = PhoneVerificationStatus.Expired;
    }

    /// <summary>Records a wrong-code attempt, locking the challenge out once <see cref="MaxAttempts"/> is reached.</summary>
    /// <returns><see langword="true"/> if this attempt caused a lockout.</returns>
    /// <exception cref="InvalidOperationException">The challenge is not <see cref="PhoneVerificationStatus.Pending"/>.</exception>
    public bool RecordFailedAttempt()
    {
        if (Status != PhoneVerificationStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending challenge can record an attempt.");
        }

        AttemptCount++;
        if (AttemptCount >= MaxAttempts)
        {
            Status = PhoneVerificationStatus.Failed;
            return true;
        }

        return false;
    }

    /// <summary>Marks the challenge successfully verified.</summary>
    /// <param name="now">The current time.</param>
    /// <exception cref="InvalidOperationException">The challenge is not <see cref="PhoneVerificationStatus.Pending"/>.</exception>
    public void MarkVerified(DateTimeOffset now)
    {
        if (Status != PhoneVerificationStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending challenge can be verified.");
        }

        Status = PhoneVerificationStatus.Verified;
        VerifiedAt = now;
    }
}
