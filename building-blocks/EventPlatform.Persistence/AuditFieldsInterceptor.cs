namespace EventPlatform.Persistence;

/// <summary>
/// Populates the audit shadow properties <c>ApplyAuditFields</c> declares, on every save, for
/// every mapped entity.
/// </summary>
/// <remarks>
/// <para>
/// Doing this in an interceptor rather than by hand is the whole point. Coverage before ADR-0036
/// was 8 of 34 entities precisely because each factory stamped its own timestamp and some were
/// never updated; a change-tracker walk cannot be forgotten when the next entity is added.
/// </para>
/// <para>
/// Built per scope, because <see cref="IAuditContext"/> is — attach it with <c>UseAuditFields</c>
/// rather than by hand.
/// </para>
/// </remarks>
/// <param name="auditContext">The actor responsible for the writes in this scope.</param>
public sealed class AuditFieldsInterceptor(IAuditContext auditContext) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        // Both overloads, not just the async one: a synchronous SaveChanges anywhere in the
        // platform would otherwise write a row with four null audit columns and no indication
        // anything was skipped.
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var actor = auditContext.Actor;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    SetIfShadow(entry, AuditFieldNames.CreatedAt, now);
                    SetIfShadow(entry, AuditFieldNames.CreatedBy, actor);
                    SetIfShadow(entry, AuditFieldNames.UpdatedAt, now);
                    SetIfShadow(entry, AuditFieldNames.UpdatedBy, actor);
                    break;

                case EntityState.Modified:
                    SetIfShadow(entry, AuditFieldNames.UpdatedAt, now);
                    SetIfShadow(entry, AuditFieldNames.UpdatedBy, actor);
                    break;

                default:
                    break;
            }
        }
    }

    // Shadow properties only, and that single condition is the whole safety story.
    //
    // ApplyAuditFields adds a shadow property exactly where the entity does not already declare a
    // real one, so "is a shadow property" is precisely the set this interceptor owns. Order,
    // Payment, PromoCode and Identity's five entities keep the CreatedAt their domain factories
    // set; QueueSettings keeps both of its real timestamps. Owned types and the outbox, which the
    // convention skipped entirely, have no property of this name at all and fall out on the null
    // check. No list of exceptions to keep in step with the model.
    private static void SetIfShadow(EntityEntry entry, string name, object? value)
    {
        if (entry.Metadata.FindProperty(name)?.IsShadowProperty() != true)
        {
            return;
        }

        entry.Property(name).CurrentValue = value;
    }
}
