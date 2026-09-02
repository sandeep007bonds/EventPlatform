namespace Venues.Domain;

/// <summary>Physical properties of a seat that a buyer needs to know before choosing it.</summary>
/// <remarks>
/// Flags rather than a single kind, because these genuinely combine: an accessible seat on an aisle
/// with a restricted view is one seat, not three mutually-exclusive ones.
/// </remarks>
[Flags]
public enum SeatAttributes
{
    /// <summary>An ordinary seat.</summary>
    None = 0,

    /// <summary>A wheelchair space or a seat with step-free access.</summary>
    Accessible = 1,

    /// <summary>Reserved for someone accompanying an <see cref="Accessible"/> seat.</summary>
    Companion = 2,

    /// <summary>The stage is partly obscured from here. Must be disclosed before purchase.</summary>
    RestrictedView = 4,

    /// <summary>Sits on an aisle.</summary>
    Aisle = 8,
}
