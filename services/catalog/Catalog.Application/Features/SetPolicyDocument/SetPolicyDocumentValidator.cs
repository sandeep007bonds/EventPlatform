namespace Catalog.Application.Features.SetPolicyDocument;

/// <summary>Validation rules for <see cref="SetPolicyDocumentCommand"/>.</summary>
public sealed class SetPolicyDocumentValidator : AbstractValidator<SetPolicyDocumentCommand>
{
    /// <summary>The longest policy body accepted, in characters.</summary>
    /// <remarks>
    /// Generous — a full terms-of-sale document with a schedule of fees is not short — but bounded,
    /// because this column has no length limit in the database and an unbounded write endpoint is
    /// a denial-of-service tool wearing a legal hat.
    /// </remarks>
    public const int MaxBodyLength = 200_000;

    /// <summary>Initializes the validation rules.</summary>
    public SetPolicyDocumentValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.Kind).IsInEnum();
        RuleFor(command => command.BodyHtml).NotEmpty().MaximumLength(MaxBodyLength);
    }
}
