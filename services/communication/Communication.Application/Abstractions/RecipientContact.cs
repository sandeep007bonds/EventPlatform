namespace Communication.Application.Abstractions;

/// <summary>A resolved recipient's contact info, as much as is known.</summary>
/// <param name="Email">The recipient's email address, if known.</param>
/// <param name="Phone">The recipient's phone number (E.164), if known.</param>
public sealed record RecipientContact(string? Email, string? Phone);
