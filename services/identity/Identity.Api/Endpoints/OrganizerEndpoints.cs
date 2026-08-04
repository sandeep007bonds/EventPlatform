namespace Identity.Api.Endpoints;

/// <summary>Maps the organizer-facing registration/login endpoints (ADR-0023).</summary>
public static class OrganizerEndpoints
{
    /// <summary>Maps the organizer endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapOrganizerEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/v1/organizers/register", RegisterAsync).WithName("RegisterOrganizer").WithTags("Organizers").AllowAnonymous();
        app.MapPost("/v1/organizers/login", LoginAsync).WithName("LoginOrganizer").WithTags("Organizers").AllowAnonymous();

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterOrganizerRequest request,
        RegisterOrganizerHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RegisterOrganizerCommand(request.OrganizationName, request.Email, request.Password),
            cancellationToken);

        return result.Outcome switch
        {
            RegisterOrganizerOutcome.Registered => Results.Created(
                $"/v1/organizers/{result.OrganizerId}",
                new OrganizerAuthResponse(
                    result.Token!.AccessToken, "Bearer", result.Token.ExpiresAt, result.OrganizerId!.Value, result.TenantId!.Value)),
            RegisterOrganizerOutcome.EmailAlreadyRegistered => Results.Json(
                new { error = "email_already_registered" }, statusCode: StatusCodes.Status409Conflict),
            _ => Results.BadRequest(new { error = "validation_failed" }),
        };
    }

    private static async Task<IResult> LoginAsync(
        LoginOrganizerRequest request,
        LoginOrganizerHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new LoginOrganizerCommand(request.Email, request.Password), cancellationToken);

        return result.Outcome switch
        {
            LoginOrganizerOutcome.LoggedIn => Results.Ok(new OrganizerAuthResponse(
                result.Token!.AccessToken, "Bearer", result.Token.ExpiresAt, result.OrganizerId!.Value, result.TenantId!.Value)),
            LoginOrganizerOutcome.LockedOut => Results.Json(new { error = "locked_out" }, statusCode: StatusCodes.Status423Locked),
            _ => Results.Json(new { error = "invalid_credentials" }, statusCode: StatusCodes.Status401Unauthorized),
        };
    }
}
