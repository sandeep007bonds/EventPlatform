namespace EventPlatform.Persistence;

/// <summary>
/// Applies EF Core migrations as an explicit, separate step — never as a side effect of a service
/// starting up.
/// <para>
/// A service that migrates on boot races itself the moment it runs more than one replica, and a bad
/// migration takes the application down with it rather than failing a job someone can look at. So
/// the same container image does both roles and the argument picks one: run it normally and it
/// serves traffic and never touches the schema; run it with <c>--migrate</c> and it applies
/// migrations, exits, and serves nothing. In Kubernetes that is an Argo CD PreSync job, so the
/// schema is in place before any new pod rolls (ADR-0029).
/// </para>
/// </summary>
public static class MigrationRunner
{
    /// <summary>The argument that switches a service from serving traffic to applying migrations.</summary>
    public const string MigrateArgument = "--migrate";

    /// <summary>Whether the process was asked to apply migrations and exit.</summary>
    /// <param name="args">The process arguments.</param>
    /// <returns><see langword="true"/> if <c>--migrate</c> was passed.</returns>
    public static bool IsMigrationRun(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return Array.Exists(args, arg => string.Equals(arg, MigrateArgument, StringComparison.Ordinal));
    }

    /// <summary>
    /// Applies every pending migration for <typeparamref name="TContext"/> and returns. Safe to run
    /// repeatedly: EF skips migrations already recorded in the history table, so a re-run against an
    /// up-to-date database is a no-op.
    /// </summary>
    /// <typeparam name="TContext">The service's database context.</typeparam>
    /// <param name="services">The built service provider.</param>
    /// <returns>A task that completes once the database is up to date.</returns>
    public static async Task ApplyMigrationsAsync<TContext>(IServiceProvider services)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(MigrationRunner));

        await EnsureHistoryTableAsync(context, logger, typeof(TContext).Name);

        var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("{Context}: database is up to date, nothing to apply.", typeof(TContext).Name);
            return;
        }

        logger.LogInformation(
            "{Context}: applying {Count} migration(s): {Migrations}.",
            typeof(TContext).Name,
            pending.Count,
            string.Join(", ", pending));

        await context.Database.MigrateAsync();

        logger.LogInformation("{Context}: migrations applied.", typeof(TContext).Name);
    }

    // Creates __EFMigrationsHistory before anything reads it, purely so a first run against an empty
    // database stops printing errors it then ignores.
    //
    // Npgsql never checks whether that table exists: Postgres's search_path decides what an
    // unqualified table name resolves to, so a probe cannot answer reliably, and
    // NpgsqlHistoryRepository.ExistsAsync therefore returns true unconditionally — it runs the
    // SELECT, lets it come back 42P01, and catches that. The exception is handled and the migration
    // proceeds correctly, but EF's command logger has already reported it at Error level. That is
    // the whole story behind the two `fail: ...Command[20102]` blocks each service used to print on
    // a fresh database before succeeding.
    //
    // Creating the table here is what MigrateAsync does moments later anyway, and the DDL it emits
    // is a create-if-not-exists, so a re-run against a live database is a no-op.
    private static async Task EnsureHistoryTableAsync(DbContext context, ILogger logger, string contextName)
    {
        try
        {
            await context.GetService<IHistoryRepository>().CreateIfNotExistsAsync();
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            // The one case this cannot pre-empt: there is no database to create the table in.
            // MigrateAsync creates the database and its history table itself, so this is a normal
            // first-run state and not a failure — said plainly here rather than as a stack trace.
            logger.LogInformation(
                "{Context}: database does not exist yet — it will be created along with the schema.",
                contextName);
        }
    }
}
