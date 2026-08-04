namespace Identity.Application.Organizers;

/// <summary>
/// Verifies an organizer's email+password and, on success, issues a token. Deliberately returns
/// the same <see cref="LoginOrganizerOutcome.InvalidCredentials"/> outcome whether the email is
/// unregistered or the password is wrong — standard anti-enumeration practice, consistent with how
/// this codebase never reveals another tenant's resource existence elsewhere (e.g. seat-map
/// tenant-mismatch 404s).
/// </summary>
/// <param name="repository">The organizer repository.</param>
/// <param name="hasher">The password hasher.</param>
/// <param name="tokenIssuer">The token issuer.</param>
public sealed class LoginOrganizerHandler(IOrganizerRepository repository, IPasswordHasher hasher, ITokenIssuer tokenIssuer)
{
    /// <summary>Handles a <see cref="LoginOrganizerCommand"/>.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the login attempt.</returns>
    public async Task<LoginOrganizerResult> HandleAsync(LoginOrganizerCommand command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var account = await repository.GetOrganizerByEmailAsync(command.Email, cancellationToken);
        if (account is null)
        {
            return new LoginOrganizerResult(LoginOrganizerOutcome.InvalidCredentials, null, null, null);
        }

        if (account.IsLockedOut(now))
        {
            return new LoginOrganizerResult(LoginOrganizerOutcome.LockedOut, null, null, null);
        }

        if (!hasher.Verify(account, command.Password))
        {
            var lockedOut = account.RecordFailedLogin(now);
            await repository.SaveChangesAsync(cancellationToken);
            return new LoginOrganizerResult(
                lockedOut ? LoginOrganizerOutcome.LockedOut : LoginOrganizerOutcome.InvalidCredentials,
                null,
                null,
                null);
        }

        account.RecordSuccessfulLogin(now);
        await repository.SaveChangesAsync(cancellationToken);

        var token = await tokenIssuer.IssueAsync(account.Id, "organizer", account.TenantId, cancellationToken);
        return new LoginOrganizerResult(LoginOrganizerOutcome.LoggedIn, token, account.Id, account.TenantId);
    }
}
