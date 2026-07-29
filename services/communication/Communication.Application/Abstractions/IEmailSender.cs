namespace Communication.Application.Abstractions;

/// <summary>
/// Port for sending a single rendered email. Implemented in the Infrastructure layer by a
/// dev/logging sender and one or more vendor adapters, selected by configuration.
/// </summary>
public interface IEmailSender
{
    /// <summary>The vendor name this implementation reports on delivery-log rows, e.g. <c>"acs"</c>, <c>"dev-log"</c>.</summary>
    string Provider { get; }

    /// <summary>Sends a rendered email.</summary>
    /// <param name="toAddress">The recipient email address.</param>
    /// <param name="subject">The rendered subject line.</param>
    /// <param name="htmlBody">The rendered HTML body.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The send outcome.</returns>
    Task<SendResult> SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken);
}
