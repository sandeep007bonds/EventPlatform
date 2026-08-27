namespace EventPlatform.Hosting;

/// <summary>
/// Resolves the audit actor from the current request, falling back to the service's own identity
/// where there is no request.
/// </summary>
/// <remarks>
/// The fallback is the interesting half. A large share of this platform's writes happen with no
/// <c>HttpContext</c> at all — the checkout saga's workflow activities, the expired-hold reaper,
/// the queue admission controller, and every Dapr pub/sub subscriber, since the sidecar delivers
/// with no user token. Attributing those to a null user would be worse than useless, so they are
/// recorded as the service that made them (ADR-0036).
/// </remarks>
/// <param name="httpContextAccessor">Accessor for the current request, if there is one.</param>
/// <param name="serviceName">The logical service name, as passed to <c>AddServiceDefaults</c>.</param>
internal sealed class HttpAuditContext(IHttpContextAccessor httpContextAccessor, string serviceName)
    : IAuditContext
{
    private readonly string serviceActor = $"service:{serviceName}";

    /// <inheritdoc />
    public string Actor => Subject ?? serviceActor;

    /// <inheritdoc />
    public ActorType ActorType => Subject is null ? ActorType.Service : ActorType.User;

    // The `sub` claim keeps its name only because AuthenticationExtensions turns MapInboundClaims
    // off; with the default on it would arrive as ...claims/nameidentifier and every audited write
    // would silently attribute to the service instead of the person who made it.
    private string? Subject
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var subject = principal.FindFirstValue(EventPlatformClaims.Subject);
            return string.IsNullOrWhiteSpace(subject) ? null : subject;
        }
    }
}
