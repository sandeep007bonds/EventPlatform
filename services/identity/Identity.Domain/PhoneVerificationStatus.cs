namespace Identity.Domain;

/// <summary>Lifecycle state of one OTP challenge.</summary>
public enum PhoneVerificationStatus
{
    /// <summary>Issued, not yet verified, not yet expired or locked.</summary>
    Pending,

    /// <summary>The submitted code matched — this challenge is consumed.</summary>
    Verified,

    /// <summary>Found past its TTL when a verify was attempted (lazy transition — see Identity.Application).</summary>
    Expired,

    /// <summary>Attempt count reached the lockout threshold; this challenge can never verify.</summary>
    Failed,
}
