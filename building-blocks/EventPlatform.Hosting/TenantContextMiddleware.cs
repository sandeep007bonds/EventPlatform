using System.Security.Claims;

namespace EventPlatform.Hosting;

/// <summary>
/// Reads the <c>tenant_id</c> claim from the authenticated principal and populates
/// the scoped <see cref="TenantContext"/> for the duration of the request.
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
internal sealed class TenantContextMiddleware(RequestDelegate next)
{
    private const string TenantClaim = "tenant_id";

    /// <summary>Executes the middleware for the current request.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="tenant">The scoped tenant context to populate.</param>
    public async Task InvokeAsync(HttpContext context, TenantContext tenant)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tenant);

        var claim = context.User.FindFirstValue(TenantClaim);
        if (Guid.TryParse(claim, out var tenantId))
        {
            tenant.TenantId = tenantId;
        }

        await next(context);
    }
}
