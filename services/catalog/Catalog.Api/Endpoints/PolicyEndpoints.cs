namespace Catalog.Api.Endpoints;

/// <summary>Maps the Catalog HTTP endpoints for terms, privacy and refund documents.</summary>
public static class PolicyEndpoints
{
    /// <summary>Maps the <c>/v1/policies</c> and <c>/v1/events/{eventId}/policies</c> endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var defaults = app.MapGroup("/v1/policies").WithTags("Policies");
        defaults.MapGet("/", GetTenantPoliciesAsync).WithName("GetTenantPolicies").RequireOrganizer();
        defaults.MapPut("/{kind}", SetTenantPolicyAsync).WithName("SetTenantPolicy").RequireOrganizer();

        var perEvent = app.MapGroup("/v1/events/{eventId:guid}/policies").WithTags("Policies");

        // Anonymous: a buyer has to be able to read the refund policy before deciding to buy, and
        // often before logging in at all. The handler still applies Event.IsVisibleTo, so a draft
        // event's documents are not readable by the public.
        perEvent.MapGet("/", GetEventPoliciesAsync).WithName("GetEventPolicies").AllowAnonymous();
        perEvent.MapPut("/{kind}", SetEventPolicyAsync).WithName("SetEventPolicy").RequireOrganizer();

        return app;
    }

    private static async Task<IResult> GetTenantPoliciesAsync(
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        var result = await sender.Send(new GetPolicyDocumentsQuery(tenant.TenantId.Value, null), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetEventPoliciesAsync(
        Guid eventId,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetEventPoliciesQuery(eventId, tenant.TenantId), cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static Task<IResult> SetTenantPolicyAsync(
        string kind,
        SetPolicyDocumentRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        SetPolicyAsync(kind, null, request, tenant, sender, cancellationToken);

    private static Task<IResult> SetEventPolicyAsync(
        Guid eventId,
        string kind,
        SetPolicyDocumentRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken) =>
        SetPolicyAsync(kind, eventId, request, tenant, sender, cancellationToken);

    private static async Task<IResult> SetPolicyAsync(
        string kind,
        Guid? eventId,
        SetPolicyDocumentRequest request,
        ITenantContext tenant,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (tenant.TenantId is null)
        {
            return Results.Unauthorized();
        }

        if (!Enum.TryParse<PolicyKind>(kind, ignoreCase: true, out var parsed))
        {
            return Results.NotFound(new { message = "No such policy document." });
        }

        var command = new SetPolicyDocumentCommand(tenant.TenantId.Value, eventId, parsed, request.BodyHtml);
        var result = await sender.Send(command, cancellationToken);

        return result.Outcome switch
        {
            SetPolicyDocumentOutcome.Saved => Results.Ok(new { version = result.Version }),
            SetPolicyDocumentOutcome.EventNotFound => Results.NotFound(),
            SetPolicyDocumentOutcome.BodyEmptyAfterSanitising =>
                Results.BadRequest(new
                {
                    message = "Nothing readable was left after removing scripts and unsupported markup.",
                }),
            _ => Results.Problem("Unexpected set-policy outcome."),
        };
    }
}
