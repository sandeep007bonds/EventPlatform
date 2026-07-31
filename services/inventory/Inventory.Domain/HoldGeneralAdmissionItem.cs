namespace Inventory.Domain;

/// <summary>
/// A quantity of one general-admission allocation that is part of a <see cref="Hold"/> — the
/// counter-based analogue of <see cref="HoldItem"/> for a section with no individual seats.
/// </summary>
public sealed class HoldGeneralAdmissionItem
{
    internal HoldGeneralAdmissionItem(Guid holdId, Guid generalAdmissionAllocationId, int quantity)
    {
        HoldId = holdId;
        GeneralAdmissionAllocationId = generalAdmissionAllocationId;
        Quantity = quantity;
    }

    // Parameterless ctor for EF Core materialization.
    private HoldGeneralAdmissionItem()
    {
    }

    /// <summary>The hold this item belongs to.</summary>
    public Guid HoldId { get; private set; }

    /// <summary>The held general-admission allocation.</summary>
    public Guid GeneralAdmissionAllocationId { get; private set; }

    /// <summary>Number of admissions held from this allocation.</summary>
    public int Quantity { get; private set; }
}
