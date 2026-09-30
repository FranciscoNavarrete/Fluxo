using FluentValidation;
using Models.Requests;

namespace Models.Validators;

public class CrearSuscripcionRedirectValidator : AbstractValidator<CrearSuscripcionRedirectRequest>
{
    public CrearSuscripcionRedirectValidator()
    {
        RuleFor(x => x.ClienteId)
            .GreaterThan(0).WithMessage("El ClienteId es obligatorio.");

        RuleFor(x => x.MpPlanId)
            .GreaterThan(0).WithMessage("El MpPlanId es obligatorio.");

        RuleFor(x => x.PayerEmail)
            .EmailAddress().WithMessage("El email del pagador no tiene un formato válido.")
            .MaximumLength(256).WithMessage("El email no puede superar 256 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.PayerEmail));

        RuleFor(x => x.BackUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("BackUrl debe ser una URL absoluta válida.")
            .When(x => !string.IsNullOrWhiteSpace(x.BackUrl));
    }
}
