namespace EventPlatform.Persistence;

/// <summary>
/// Translates provider-specific database errors into the conditions the domain actually cares
/// about.
/// <para>
/// This exists because the alternative leaks badly. Catching <c>PostgresException</c> inline — as
/// Ordering and Payments both used to — compiles and passes every test on any other engine while
/// silently doing the wrong thing: the <c>catch</c> clause simply never matches, the exception
/// escapes, and a concurrent duplicate checkout or charge turns into a 500 instead of the intended
/// 409. That is a correctness failure in the money path that no compiler would ever flag, so the
/// provider knowledge lives here, in one file, rather than scattered through the services
/// (ADR-0029).
/// </para>
/// </summary>
public static class DbExceptions
{
    /// <summary>
    /// Whether a save failed because it violated a unique index — i.e. a concurrent writer won the
    /// race for the same key. Callers treat this as "the other one won", not as an error.
    /// </summary>
    /// <param name="exception">The exception thrown by <c>SaveChangesAsync</c>.</param>
    /// <returns><see langword="true"/> if the cause was a unique-constraint violation.</returns>
    public static bool IsUniqueViolation(this DbUpdateException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        };
    }
}
