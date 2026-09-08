namespace EventPlatform.Persistence;

/// <summary>
/// Model-building helper declaring what is already true: every key in this platform is minted by
/// the code, never by the database.
/// </summary>
public static class ClientGeneratedKeyModelBuilderExtensions
{
    /// <summary>
    /// Marks every single-property <see cref="Guid"/> primary key as
    /// <c>ValueGeneratedNever</c>. Call from a DbContext's <c>OnModelCreating</c> after
    /// <c>ApplyConfigurationsFromAssembly</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// EF's default for a key it believes the store generates is <c>ValueGeneratedOnAdd</c>, and
    /// that default decides something far less obvious than where an id comes from: <b>whether a
    /// newly discovered entity is an INSERT or an UPDATE</b>. When EF finds an untracked entity
    /// hanging off a tracked parent, it asks whether the key is set. Unset means new; set means
    /// EF concludes the row already exists and marks it <c>Modified</c>.
    /// </para>
    /// <para>
    /// Every aggregate here assigns <c>Guid.CreateVersion7()</c> in its constructor, so the key is
    /// always set — and EF therefore treated every child added to an already-loaded aggregate as an
    /// existing row. It issued <c>UPDATE … WHERE "Id" = &lt;a guid no row has&gt;</c>, which affects
    /// zero rows, which surfaces as <c>DbUpdateConcurrencyException</c>: "expected to affect 1
    /// row(s), but actually affected 0". Nothing about the message points at the cause, and it
    /// blames concurrency for what is a misread intent.
    /// </para>
    /// <para>
    /// It only bites on the <b>second</b> write. A brand-new aggregate is <c>Add</c>ed as a whole
    /// graph, so everything in it is <c>Added</c> and inserted. Load that aggregate back and give
    /// it a new child and the heuristic applies — which is why the seat-map editor saved a fresh
    /// draft and failed on every edit after it.
    /// </para>
    /// <para>
    /// <c>ValueGeneratedNever</c> removes the question. A discovered entity with no store-generated
    /// key is <c>Added</c>, full stop. It is also simply the truth: no table here has a default or
    /// an identity on its key column, so this changes no DDL — only what EF infers from it.
    /// </para>
    /// </remarks>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The same <paramref name="modelBuilder"/> for chaining.</returns>
    public static ModelBuilder ApplyClientGeneratedKeys(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Composite keys are left alone: they are made of foreign keys and discriminators that
            // the store never generates either, so there is nothing to correct and no heuristic to
            // disarm.
            if (entityType.FindPrimaryKey() is not { Properties: [{ ClrType: var clrType } key] }
                || clrType != typeof(Guid))
            {
                continue;
            }

            key.ValueGenerated = ValueGenerated.Never;
        }

        return modelBuilder;
    }
}
