namespace EventPlatform.Hosting;

/// <summary>
/// Scoped, mutable holder for the current tenant, populated by <c>TenantContextMiddleware</c>.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    /// <inheritdoc />
    public Guid? TenantId { get; internal set; }
}
