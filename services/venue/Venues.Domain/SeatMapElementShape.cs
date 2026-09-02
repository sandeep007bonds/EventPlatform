namespace Venues.Domain;

/// <summary>The geometry a <see cref="SeatMapElement"/> is drawn with.</summary>
public enum SeatMapElementShape
{
    /// <summary>An axis-aligned box, positioned and sized by the element's bounds.</summary>
    Rectangle = 0,

    /// <summary>An ellipse inscribed in the element's bounds.</summary>
    Ellipse = 1,

    /// <summary>A closed polygon through the element's points.</summary>
    Polygon = 2,

    /// <summary>An open path through the element's points — an aisle, a shoreline.</summary>
    Path = 3,
}
