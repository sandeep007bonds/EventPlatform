namespace Communication.Application.Abstractions;

/// <summary>
/// Port for sending a single WhatsApp message. Implemented in the Infrastructure layer by a
/// dev/logging sender and one or more vendor adapters, selected by configuration.
/// </summary>
public interface IWhatsAppSender
{
    /// <summary>The vendor name this implementation reports on delivery-log rows, e.g. <c>"acs"</c>, <c>"twilio"</c>, <c>"dev-log"</c>.</summary>
    string Provider { get; }

    /// <summary>Sends a plain-text WhatsApp message.</summary>
    /// <param name="toPhoneNumber">The recipient WhatsApp-registered phone number, in E.164 format.</param>
    /// <param name="body">The message body.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The send outcome.</returns>
    Task<SendResult> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken);
}
