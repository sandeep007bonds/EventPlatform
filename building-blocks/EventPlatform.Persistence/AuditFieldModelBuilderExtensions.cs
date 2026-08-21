namespace EventPlatform.Persistence;

/// <summary>Model-building helpers for the audit shadow properties (ADR-0036).</summary>
public static class AuditFieldModelBuilderExtensions
{
    /// <summary>Table name <c>ApplyOutbox</c> maps the outbox to; excluded from audit fields.</summary>
    private const string OutboxTableName = "outbox";

    /// <summary>
    /// Adds <c>CreatedAt</c>, <c>CreatedBy</c>, <c>UpdatedAt</c> and <c>UpdatedBy</c> as shadow
    /// properties to every mapped entity type. Call from a DbContext's <c>OnModelCreating</c>,
    /// <b>after</b> <c>ApplyConfigurationsFromAssembly</c> and <c>ApplyOutbox</c> — the model must
    /// be fully populated, and the outbox must already have its table name for the skip below to
    /// recognise it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Shadow, not entity properties, because every <c>*.Domain</c> project in this repo has zero
    /// project references and audit metadata is not a domain concern — see ADR-0036. The values are
    /// populated by the audit interceptor, never by domain code.
    /// </para>
    /// <para>
    /// Applying this by convention rather than per entity is the point: coverage today is 8 of 34
    /// entities precisely because each factory sets its own timestamp by hand and some were never
    /// updated. A convention cannot be forgotten when the next entity is added.
    /// </para>
    /// <para>
    /// Skipped: owned types, which have no identity of their own and are audited as part of their
    /// owner; entities that already declare a real property of the same name, which keeps the
    /// existing load-bearing <c>CreatedAt</c> on <c>Order</c>, <c>Payment</c>, <c>PromoCode</c> and
    /// Identity's entities exactly as it is; and the outbox table, which is transport plumbing
    /// rather than business data, is pruned once published, and is high-volume enough that four
    /// unused columns per row are worth avoiding.
    /// </para>
    /// </remarks>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The same <paramref name="modelBuilder"/> for chaining.</returns>
    public static ModelBuilder ApplyAuditFields(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Matched on table name rather than on the CLR type: this project deliberately has no
            // project references, and reaching for OutboxMessage would make the persistence
            // building block depend on the messaging one — the wrong direction.
            if (entityType.IsOwned()
                || string.Equals(entityType.GetTableName(), OutboxTableName, StringComparison.Ordinal))
            {
                continue;
            }

            var builder = modelBuilder.Entity(entityType.ClrType);

            AddTimestamp(builder, entityType, AuditFieldNames.CreatedAt);
            AddTimestamp(builder, entityType, AuditFieldNames.UpdatedAt);
            AddActor(builder, entityType, AuditFieldNames.CreatedBy);
            AddActor(builder, entityType, AuditFieldNames.UpdatedBy);
        }

        return modelBuilder;
    }

    // FindProperty returns non-null when the entity already declares a real CLR property of that
    // name; adding a shadow property over it would throw. Leaving the real one alone is deliberate.
    private static void AddTimestamp(EntityTypeBuilder builder, IMutableEntityType entityType, string name)
    {
        if (entityType.FindProperty(name) is not null)
        {
            return;
        }

        builder.Property<DateTimeOffset?>(name);
    }

    private static void AddActor(EntityTypeBuilder builder, IMutableEntityType entityType, string name)
    {
        if (entityType.FindProperty(name) is not null)
        {
            return;
        }

        // Nullable and 200 chars: an actor is a user id GUID, or a service identity such as
        // "service:ordering-checkout-saga" for the writes no person made. Null only for rows
        // written before this convention existed — a new row always has one.
        builder.Property<string?>(name).HasMaxLength(200);
    }
}
