namespace Identity.Application.Otp;

/// <summary>Outcome of a <see cref="RequestOtpCommand"/>.</summary>
public enum RequestOtpOutcome
{
    /// <summary>A code was generated and handed to Communication successfully.</summary>
    Sent,

    /// <summary>Too soon since the last code for this phone number; retry after the cooldown.</summary>
    RateLimited,

    /// <summary>Communication rejected or failed the send.</summary>
    SendFailed,

    /// <summary>The phone number failed E.164 validation.</summary>
    InvalidPhoneNumber,
}
