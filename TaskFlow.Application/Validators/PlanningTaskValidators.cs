using FluentValidation;

namespace TaskFlow.Application.Validators;

public class CreatePlanningTaskValidator : AbstractValidator<DTOs.CreatePlanningTaskDto>
{
    public CreatePlanningTaskValidator()
    {
        RuleFor(x => x.TaskGroupId)
            .NotEmpty().WithMessage("El grupo de tareas es obligatorio.");

        RuleFor(x => x.Number)
            .GreaterThanOrEqualTo(1).WithMessage("El número de tarea debe ser mayor o igual a 1.");

        RuleFor(x => x.SubNumber)
            .GreaterThanOrEqualTo(1).When(x => x.SubNumber.HasValue)
            .WithMessage("El subnúmero debe ser mayor o igual a 1.");

        RuleFor(x => x.SubNumber)
            .Null().When(x => x.Number < 1);

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(4000).WithMessage("La descripción no debe exceder los 4000 caracteres.");
    }
}

public class UpdatePlanningTaskValidator : AbstractValidator<DTOs.UpdatePlanningTaskDto>
{
    public UpdatePlanningTaskValidator()
    {
        RuleFor(x => x.Number)
            .GreaterThanOrEqualTo(1).WithMessage("El número de tarea debe ser mayor o igual a 1.");

        RuleFor(x => x.SubNumber)
            .GreaterThanOrEqualTo(1).When(x => x.SubNumber.HasValue)
            .WithMessage("El subnúmero debe ser mayor o igual a 1.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(4000).WithMessage("La descripción no debe exceder los 4000 caracteres.");
    }
}