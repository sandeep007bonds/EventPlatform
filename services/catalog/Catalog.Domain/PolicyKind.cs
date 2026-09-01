namespace Catalog.Domain;

/// <summary>The kinds of legal document an organizer publishes alongside an event.</summary>
public enum PolicyKind
{
    /// <summary>Terms and conditions of sale.</summary>
    Terms,

    /// <summary>Privacy notice covering the data collected from a buyer.</summary>
    Privacy,

    /// <summary>Refund, exchange and cancellation policy.</summary>
    Refund,
}
