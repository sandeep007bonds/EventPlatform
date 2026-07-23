namespace EventPlatform.Hosting;

/// <summary>
/// Provides the current request's tenant identity, resolved from the validated JWT.
/// Per ADR-0011, the tenant is trusted only from the token claim, never from the request body.
/// </summary>
public interface ITenantContext
{
    /// <summary>The current tenant id, or <see langword="null"/> when the request is unauthenticated.</summary>
    Guid? TenantId { get; }
}
