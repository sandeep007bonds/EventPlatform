namespace EventPlatform.Persistence;

/// <summary>
/// Names of the audit shadow properties every mapped entity carries.
/// </summary>
/// <remarks>
/// Shadow properties have no compiler-checked identity — they are addressed by string — so the
/// strings live here rather than being spelled at each call site. Read one with
/// <c>context.Entry(entity).Property(AuditFieldNames.CreatedAt)</c>, or in a LINQ query with
/// <c>EF.Property&lt;DateTimeOffset&gt;(e, AuditFieldNames.CreatedAt)</c>.
/// </remarks>
public static class AuditFieldNames
{
    /// <summary>When the row was first written (UTC).</summary>
    public const string CreatedAt = "CreatedAt";

    /// <summary>Who first wrote the row — a user id, or a service identity for saga/background writes.</summary>
    public const string CreatedBy = "CreatedBy";

    /// <summary>When the row was last written (UTC). Equal to <see cref="CreatedAt"/> until first update.</summary>
    public const string UpdatedAt = "UpdatedAt";

    /// <summary>Who last wrote the row.</summary>
    public const string UpdatedBy = "UpdatedBy";
}
