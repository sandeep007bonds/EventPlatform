namespace Identity.Application.Otp;

/// <summary>Outcome of a <see cref="VerifyOtpCommand"/>.</summary>
public enum VerifyOtpOutcome
{
    /// <summary>The code matched; an access token was issued.</summary>
    Verified,

    /// <summary>No pending challenge exists for this phone number.</summary>
    NoActiveChallenge,

    /// <summary>The latest challenge for this phone number has expired.</summary>
    Expired,

    /// <summary>The submitted code did not match.</summary>
    WrongCode,

    /// <summary>The challenge is locked out after too many wrong attempts.</summary>
    LockedOut,
}
