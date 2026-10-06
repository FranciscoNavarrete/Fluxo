using FluentValidation;
using Models.Requests;

namespace Models.Validators;

public class CrearPlanValidator : AbstractValidator<CrearPlanRequest>
{
    private static readonly string[] _frecuenciasValidas = ["months", "days"];
    private static readonly string[] _monedasValidas = ["ARS", "USD", "BRL", "MXN", "CLP", "COP", "PEN"];

    public CrearPlanValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del plan es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .MaximumLength(500).WithMessage("La descripción no puede superar 500 caracteres.")
            .When(x => x.Descripcion != null);

        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");

        RuleFor(x => x.MontoPrimerCobro)
            .GreaterThan(0).WithMessage("El monto del primer cobro debe ser mayor a cero.")
            .When(x => x.MontoPrimerCobro.HasValue);

        RuleFor(x => x.MontoPromo)
            .GreaterThan(0).WithMessage("El precio promocional debe ser mayor a cero.")
            .When(x => x.MontoPromo.HasValue);

        RuleFor(x => x.MesesPromo)
            .GreaterThan(0).WithMessage("Los meses de promoción deben ser al menos 1.")
            .LessThanOrEqualTo(60).WithMessage("Los meses de promoción no pueden superar 60.")
            .When(x => x.MesesPromo.HasValue);

        RuleFor(x => x)
            .Must(x => x.MontoPromo.HasValue == x.MesesPromo.HasValue)
            .WithMessage("Para una promoción hay que indicar el precio promocional y por cuántos meses.");

        RuleFor(x => x.Moneda)
            .NotEmpty().WithMessage("La moneda es obligatoria.")
            .Must(m => _monedasValidas.Contains(m.ToUpper()))
            .WithMessage($"Moneda inválida. Valores permitidos: {string.Join(", ", _monedasValidas)}.");

        RuleFor(x => x.TipoFrecuencia)
            .NotEmpty().WithMessage("El tipo de frecuencia es obligatorio.")
            .Must(f => _frecuenciasValidas.Contains(f.ToLower()))
            .WithMessage("TipoFrecuencia debe ser 'months' o 'days'.");

        RuleFor(x => x.Frecuencia)
            .GreaterThan(0).WithMessage("La frecuencia debe ser mayor a cero.")
            .LessThanOrEqualTo(12).WithMessage("La frecuencia no puede superar 12.");

        RuleFor(x => x.DiasGratis)
            .GreaterThanOrEqualTo(0).WithMessage("Los días de prueba no pueden ser negativos.")
            .LessThanOrEqualTo(365).WithMessage("Los días de prueba no pueden superar 365.");
    }
}
