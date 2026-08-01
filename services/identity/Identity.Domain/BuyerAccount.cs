namespace Identity.Domain;

/// <summary>
/// A buyer's durable identity, keyed by phone number. Created on first successful OTP
/// verification (verification *is* signup — no separate account-creation step) so the same
/// <c>sub</c> claim is minted on every subsequent login, regardless of which organizer's event
/// the buyer is transacting with. Deliberately has no tenant — a buyer is not tenant-scoped
/// (ADR-0022).
/// </summary>
public sealed class BuyerAccount
{
    private BuyerAccount()
    {
    }

    private BuyerAccount(string phoneNumber)
    {
        Id = Guid.CreateVersion7();
        PhoneNumber = phoneNumber;
        CreatedAt = DateTimeOffset.UtcNow;
        LastVerifiedAt = CreatedAt;
    }

    /// <summary>The buyer's stable id — minted as the JWT <c>sub</c> claim.</summary>
    public Guid Id { get; private set; }

    /// <summary>The buyer's phone number, in E.164 format (unique).</summary>
    public string PhoneNumber { get; private set; } = default!;

    /// <summary>When this buyer's account was first created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When this buyer last completed an OTP verification.</summary>
    public DateTimeOffset LastVerifiedAt { get; private set; }

    /// <summary>Creates a new buyer account on first-ever verification.</summary>
    /// <param name="phoneNumber">The verified phone number.</param>
    /// <returns>A new <see cref="BuyerAccount"/>.</returns>
    public static BuyerAccount CreateFromVerification(string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        return new BuyerAccount(phoneNumber);
    }

    /// <summary>Stamps a repeat successful verification.</summary>
    /// <param name="now">The current time.</param>
    public void RecordVerification(DateTimeOffset now) => LastVerifiedAt = now;
}
