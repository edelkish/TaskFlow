using FluentValidation;

namespace TaskFlow.Application.Validators;

public class CreateDevGroupValidator : AbstractValidator<DTOs.CreateDevGroupDto>
{
    public CreateDevGroupValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del grupo es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no debe exceder los 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no debe exceder los 500 caracteres.");

        RuleFor(x => x.MemberIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("La lista de miembros no puede tener duplicados.");
    }
}

public class UpdateDevGroupValidator : AbstractValidator<DTOs.UpdateDevGroupDto>
{
    public UpdateDevGroupValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del grupo es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no debe exceder los 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no debe exceder los 500 caracteres.");
    }
}
