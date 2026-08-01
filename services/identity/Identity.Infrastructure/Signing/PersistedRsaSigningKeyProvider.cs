namespace Identity.Infrastructure.Signing;

/// <summary>
/// Generates a 2048-bit RSA key on first use and persists it to the <c>identity</c> Postgres
/// database (<c>signing_keys</c> table) so a local/dev-cluster restart doesn't invalidate every
/// outstanding buyer session. See the ADR-0016 extension for the full persistence-strategy
/// rationale and the multi-replica caveat below.
/// </summary>
/// <param name="scopeFactory">Used to resolve a scoped repository per generate-or-load call.</param>
internal sealed class PersistedRsaSigningKeyProvider(IServiceScopeFactory scopeFactory) : ISigningKeyProvider
{
    // Instance-level cache + lock: this type is registered AddSingleton (see DependencyInjection.cs),
    // so exactly one instance exists per process — an instance field already gives the
    // "generate/load once per process" guarantee, no static needed. Safe under a same-process race
    // because every service in this repo deploys with `replicas: 1` (see every
    // deploy/base/*/deployment.yaml). If replica count ever increases, first-boot key generation
    // needs a proper "insert-if-not-exists, retry on unique-constraint conflict" pattern instead of
    // this lock — not built now because it can't happen with a single replica.
    private readonly SemaphoreSlim initLock = new(1, 1);
    private ActiveSigningKey? cachedActiveKey;

    /// <inheritdoc />
    public async Task<ActiveSigningKey> GetActiveKeyAsync(CancellationToken cancellationToken)
    {
        if (cachedActiveKey is not null)
        {
            return cachedActiveKey;
        }

        await initLock.WaitAsync(cancellationToken);
        try
        {
            if (cachedActiveKey is not null)
            {
                return cachedActiveKey;
            }

            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ISigningKeyRepository>();

            var row = await repository.GetActiveAsync(cancellationToken);
            if (row is null)
            {
                row = SigningKey.Generate();
                repository.Add(row);
                await repository.SaveChangesAsync(cancellationToken);
            }

            var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(row.PrivateKeyPkcs8Base64), out _);
            cachedActiveKey = new ActiveSigningKey(row.Id.ToString(), rsa);
            return cachedActiveKey;
        }
        finally
        {
            initLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublicSigningKey>> GetPublicKeysAsync(CancellationToken cancellationToken)
    {
        var active = await GetActiveKeyAsync(cancellationToken);
        return [new PublicSigningKey(active.Kid, active.Rsa.ExportParameters(includePrivateParameters: false))];
    }
}
