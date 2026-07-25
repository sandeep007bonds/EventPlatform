namespace Payments.Application.Charging;

/// <summary>Result of a charge attempt.</summary>
public enum ChargeOutcome
{
    /// <summary>Funds were captured.</summary>
    Captured,

    /// <summary>The charge failed.</summary>
    Failed,
}
