using FluentValidation;
using Models.Requests;

namespace Models.Validators;

public class CrearSuscripcionConTokenValidator : AbstractValidator<CrearSuscripcionConTokenRequest>
{
    public CrearSuscripcionConTokenValidator()
    {
        RuleFor(x => x.ClienteId)
            .GreaterThan(0).WithMessage("El ClienteId es obligatorio.");

        RuleFor(x => x.MpPlanId)
            .GreaterThan(0).WithMessage("El MpPlanId es obligatorio.");

        RuleFor(x => x.CardTokenId)
            .MaximumLength(200).WithMessage("El card_token no puede superar 200 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.CardTokenId));

        RuleFor(x => x.PayerEmail)
            .NotEmpty().WithMessage("El email del pagador es obligatorio.")
            .EmailAddress().WithMessage("El email del pagador no tiene un formato válido.")
            .MaximumLength(256).WithMessage("El email no puede superar 256 caracteres.");

        RuleFor(x => x.PaymentMethodId)
            .MaximumLength(50).WithMessage("El PaymentMethodId no puede superar 50 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.PaymentMethodId));
    }
}
