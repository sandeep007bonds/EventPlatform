namespace EventPlatform.Contracts;

/// <summary>
/// Declares the contract version of an <see cref="IntegrationEvent"/>. Absent means version 1.
/// </summary>
/// <remarks>
/// Bump this when a change to the record would make an existing consumer read it wrongly — a field
/// removed, renamed, or given a new meaning. Adding an optional field does not need a bump, because
/// a consumer that does not know about it simply ignores it.
/// <para>
/// The point is to let producer and consumer deploy separately. Without a version on the wire the
/// only safe way to change a contract's meaning is to stop the world; with one, a consumer can
/// handle v1 and v2 side by side until the old messages have drained.
/// </para>
/// </remarks>
/// <param name="version">The contract version. Must be 1 or greater.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EventVersionAttribute(int version) : Attribute
{
    /// <summary>The version every event has unless it says otherwise.</summary>
    public const int Default = 1;

    /// <summary>The declared contract version.</summary>
    public int Version { get; } = version >= 1
        ? version
        : throw new ArgumentOutOfRangeException(nameof(version), version, "Event versions start at 1.");
}
