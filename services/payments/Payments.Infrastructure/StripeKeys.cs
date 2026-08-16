namespace Payments.Infrastructure;

/// <summary>
/// Recognises whether a configured value is actually a Stripe credential.
/// <para>
/// The gate that picks the real gateway over the simulator used to be "is this string non-empty?",
/// which is too weak in a deployed environment. Key Vault objects for the Stripe keys exist
/// unconditionally — the CSI SecretProviderClass lists them by name, and a missing object breaks
/// the mount for every pod in the namespace — so Payments always receives *something*, including
/// the placeholder Terraform writes before anyone sets a real key. Treating that placeholder as a
/// real key would hand it to Stripe and fail every checkout; treating a real key as absent would
/// silently take fake payments. Matching Stripe's documented prefixes distinguishes the two, and
/// also catches an ordinary paste error.
/// </para>
/// </summary>
internal static class StripeKeys
{
    /// <summary>Prefix on every Stripe secret API key (<c>sk_test_</c> / <c>sk_live_</c>).</summary>
    private const string SecretKeyPrefix = "sk_";

    /// <summary>Prefix on every Stripe webhook signing secret.</summary>
    private const string WebhookSecretPrefix = "whsec_";

    /// <summary>Whether <paramref name="value"/> looks like a Stripe secret API key.</summary>
    /// <param name="value">The configured value, possibly null, blank or a placeholder.</param>
    /// <returns><see langword="true"/> if it carries Stripe's secret-key prefix.</returns>
    internal static bool IsSecretKey(string? value) => HasPrefix(value, SecretKeyPrefix);

    /// <summary>Whether <paramref name="value"/> looks like a Stripe webhook signing secret.</summary>
    /// <param name="value">The configured value, possibly null, blank or a placeholder.</param>
    /// <returns><see langword="true"/> if it carries Stripe's webhook-secret prefix.</returns>
    internal static bool IsWebhookSecret(string? value) => HasPrefix(value, WebhookSecretPrefix);

    private static bool HasPrefix(string? value, string prefix) =>
        !string.IsNullOrWhiteSpace(value) && value.StartsWith(prefix, StringComparison.Ordinal);
}
