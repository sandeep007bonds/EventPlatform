namespace Communication.Application.Abstractions;

/// <summary>
/// Port for sending a single SMS message. Implemented in the Infrastructure layer by a
/// dev/logging sender and one or more vendor adapters, selected by configuration.
/// </summary>
public interface ISmsSender
{
    /// <summary>The vendor name this implementation reports on delivery-log rows, e.g. <c>"twilio"</c>, <c>"dev-log"</c>.</summary>
    string Provider { get; }

    /// <summary>Sends a plain-text SMS.</summary>
    /// <param name="toPhoneNumber">The recipient phone number, in E.164 format.</param>
    /// <param name="body">The message body.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The send outcome.</returns>
    Task<SendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken);
}
