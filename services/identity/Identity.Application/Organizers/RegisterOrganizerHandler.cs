namespace Identity.Application.Organizers;

/// <summary>
/// Registers a new organization: creates a <see cref="Tenant"/> and its first
/// <see cref="OrganizerAccount"/> together, then issues a token. Self-service signup — there is no
/// existing tenant to join; inviting additional organizers into an existing tenant is deferred
/// (ADR-0023).
/// </summary>
/// <param name="repository">The organizer repository.</param>
/// <param name="hasher">The password hasher.</param>
/// <param name="tokenIssuer">The token issuer.</param>
public sealed class RegisterOrganizerHandler(IOrganizerRepository repository, IPasswordHasher hasher, ITokenIssuer tokenIssuer)
{
    /// <summary>Handles a <see cref="RegisterOrganizerCommand"/>.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public async Task<RegisterOrganizerResult> HandleAsync(RegisterOrganizerCommand command, CancellationToken cancellationToken)
    {
        if (!RegisterOrganizerValidator.IsValid(command))
        {
            return new RegisterOrganizerResult(RegisterOrganizerOutcome.ValidationFailed, null, null, null);
        }

        var existing = await repository.GetOrganizerByEmailAsync(command.Email, cancellationToken);
        if (existing is not null)
        {
            return new RegisterOrganizerResult(RegisterOrganizerOutcome.EmailAlreadyRegistered, null, null, null);
        }

        var tenant = Tenant.Create(command.OrganizationName);
        repository.AddTenant(tenant);

        var passwordHash = hasher.Hash(command.Password);
        var account = OrganizerAccount.Register(tenant.Id, command.Email, passwordHash);
        repository.AddOrganizerAccount(account);

        await repository.SaveChangesAsync(cancellationToken);

        var token = await tokenIssuer.IssueAsync(account.Id, "organizer", tenant.Id, cancellationToken);
        return new RegisterOrganizerResult(RegisterOrganizerOutcome.Registered, token, account.Id, tenant.Id);
    }
}
