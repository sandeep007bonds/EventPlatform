namespace Identity.Api.Endpoints;

/// <summary>Maps the buyer-facing OTP request/verify endpoints.</summary>
public static class OtpEndpoints
{
    /// <summary>Maps the OTP endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapOtpEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapPost("/v1/otp/request", RequestAsync).WithName("RequestOtp").WithTags("Otp").AllowAnonymous();
        app.MapPost("/v1/otp/verify", VerifyAsync).WithName("VerifyOtp").WithTags("Otp").AllowAnonymous();

        return app;
    }

    private static async Task<IResult> RequestAsync(
        RequestOtpRequest request,
        RequestOtpHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RequestOtpCommand(request.PhoneNumber), cancellationToken);
        return result.Outcome switch
        {
            RequestOtpOutcome.Sent => Results.Ok(new RequestOtpResponse(result.ExpiresInSeconds!.Value)),
            RequestOtpOutcome.RateLimited => Results.Json(
                new { error = "rate_limited", retryAfterSeconds = result.RetryAfterSeconds },
                statusCode: StatusCodes.Status429TooManyRequests),
            RequestOtpOutcome.InvalidPhoneNumber => Results.BadRequest(new { error = "invalid_phone_number" }),
            _ => Results.Json(new { error = "send_failed" }, statusCode: StatusCodes.Status502BadGateway),
        };
    }

    private static async Task<IResult> VerifyAsync(
        VerifyOtpRequest request,
        VerifyOtpHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new VerifyOtpCommand(request.PhoneNumber, request.Code), cancellationToken);
        return result.Outcome switch
        {
            VerifyOtpOutcome.Verified => Results.Ok(new VerifyOtpResponse(
                result.Token!.AccessToken, "Bearer", result.Token.ExpiresAt, result.BuyerId!.Value)),
            VerifyOtpOutcome.LockedOut => Results.Json(new { error = "locked_out" }, statusCode: StatusCodes.Status423Locked),
            VerifyOtpOutcome.Expired => Results.BadRequest(new { error = "expired" }),
            VerifyOtpOutcome.WrongCode => Results.BadRequest(new { error = "wrong_code" }),
            _ => Results.BadRequest(new { error = "no_active_challenge" }),
        };
    }
}
