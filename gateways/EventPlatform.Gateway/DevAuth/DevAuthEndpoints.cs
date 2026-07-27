namespace EventPlatform.Gateway.DevAuth;

/// <summary>
/// Maps the Development-only dev-login endpoint. Only called from <c>Program.cs</c> when
/// <c>Jwt:DevSigningKey</c> is configured — never in Production.
/// </summary>
public static class DevAuthEndpoints
{
    /// <summary>Maps <c>POST /api/auth/dev-login</c>.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapDevAuthEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/api/auth/dev-login", DevLogin)
            .WithName("DevLogin")
            .WithTags("Auth")
            .ExcludeFromDescription();

        return app;
    }

    private static IResult DevLogin(DevLoginRequest request, IConfiguration configuration, DevTokenIssuer issuer)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new { message = "Email is required." });
        }

        var jwt = configuration.GetSection("Jwt");
        var signingKey = jwt["DevSigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            // Belt-and-braces: Program.cs only maps this endpoint when the key is present at
            // startup, but configuration can in principle change at runtime.
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var result = issuer.Issue(request, signingKey, jwt["Issuer"], jwt["Audience"]);
        var response = new DevLoginResponse(
            result.AccessToken,
            (int)(result.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds,
            "Bearer",
            result.User);

        return Results.Ok(response);
    }
}
