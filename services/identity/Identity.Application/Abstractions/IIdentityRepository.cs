namespace Identity.Application.Abstractions;

/// <summary>
/// Persistence abstraction for phone-verification challenges and buyer accounts. One repository,
/// shared <see cref="SaveChangesAsync"/>, so a successful verify (challenge update + buyer
/// upsert) lands in a single transaction — same shape as Communication's
/// <c>INotificationRepository</c>.
/// </summary>
public interface IIdentityRepository
{
    /// <summary>Registers a new challenge to be persisted.</summary>
    /// <param name="verification">The challenge to add.</param>
    void AddPhoneVerification(PhoneVerification verification);

    /// <summary>Returns the most recently issued challenge for a phone number, if any.</summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<PhoneVerification?> GetLatestPhoneVerificationAsync(string phoneNumber, CancellationToken cancellationToken);

    /// <summary>Returns the buyer account for a phone number, if one has been created yet.</summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<BuyerAccount?> GetBuyerAccountByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken);

    /// <summary>Registers a new buyer account to be persisted.</summary>
    /// <param name="account">The account to add.</param>
    void AddBuyerAccount(BuyerAccount account);

    /// <summary>Persists all pending changes.</summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
