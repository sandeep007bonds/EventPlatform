namespace Identity.Application.Abstractions;

/// <summary>
/// Sends an OTP code to a phone number, via Communication's SMS channel. The Dapr invocation
/// itself lives in Infrastructure (<c>DaprOtpSender</c>) — same layering as Ordering's
/// <c>IHoldClient</c>/<c>DaprHoldClient</c> and Inventory's <c>ISeatMapClient</c>/<c>DaprSeatMapClient</c>:
/// the port is Application-owned, the cross-service HTTP call is Infrastructure-owned.
/// </summary>
public interface IOtpSender
{
    /// <summary>Sends the code by SMS.</summary>
    /// <param name="phoneNumber">The recipient, in E.164 format.</param>
    /// <param name="code">The plaintext 6-digit code (never persisted — only sent here and hashed for storage).</param>
    /// <param name="expiresInMinutes">The code's TTL, for the message text.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if Communication accepted the send.</returns>
    Task<bool> SendAsync(string phoneNumber, string code, int expiresInMinutes, CancellationToken cancellationToken);
}
