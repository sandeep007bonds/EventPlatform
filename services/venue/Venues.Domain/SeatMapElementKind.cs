namespace Venues.Domain;

/// <summary>What a <see cref="SeatMapElement"/> depicts.</summary>
public enum SeatMapElementKind
{
    /// <summary>The outline of a <see cref="VenueSection"/>, drawn at overview zoom.</summary>
    SectionShape = 0,

    /// <summary>The outline of an <see cref="AdmissionArea"/>.</summary>
    AreaShape = 1,

    /// <summary>The stage, pitch, ring or screen — whatever the audience faces.</summary>
    Stage = 2,

    /// <summary>An entrance, marking where a <see cref="VenueGate"/> is on the plan.</summary>
    Entrance = 3,

    /// <summary>A bar, toilet, lift or other facility drawn for orientation.</summary>
    Facility = 4,

    /// <summary>A pillar, camera platform or anything else that blocks a view.</summary>
    Obstruction = 5,

    /// <summary>Free text placed on the plan.</summary>
    Label = 6,
}
