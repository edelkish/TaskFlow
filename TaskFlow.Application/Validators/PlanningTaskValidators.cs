using FluentValidation;

namespace TaskFlow.Application.Validators;

public class CreatePlanningTaskValidator : AbstractValidator<DTOs.CreatePlanningTaskDto>
{
    public CreatePlanningTaskValidator()
    {
        // Una tarea cuelga de un grupo (período) o de un proyecto (backlog), nunca de ambos
        // ni de ninguno. El servicio resuelve el proyecto final en cada caso.
        RuleFor(x => x)
            .Must(x => x.TaskGroupId.HasValue || x.ProjectId.HasValue)
            .WithMessage("Indique un grupo (tarea de período) o un proyecto (tarea de backlog).");

        RuleFor(x => x.TaskGroupId)
            .NotEmpty().WithMessage("El grupo de tareas es obligatorio.")
            .When(x => !x.ProjectId.HasValue);

        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("El número de tarea es obligatorio para tareas de período.")
            .When(x => x.TaskGroupId.HasValue);

        RuleFor(x => x.Number)
            .GreaterThanOrEqualTo(1).WithMessage("El número de tarea debe ser mayor o igual a 1.")
            .When(x => x.Number.HasValue);

        RuleFor(x => x.SubNumber)
            .GreaterThanOrEqualTo(1).When(x => x.SubNumber.HasValue)
            .WithMessage("El subnúmero debe ser mayor o igual a 1.");

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
            .GreaterThanOrEqualTo(1).When(x => x.Number.HasValue)
            .WithMessage("El número de tarea debe ser mayor o igual a 1.");

        RuleFor(x => x.SubNumber)
            .GreaterThanOrEqualTo(1).When(x => x.SubNumber.HasValue)
            .WithMessage("El subnúmero debe ser mayor o igual a 1.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .MaximumLength(4000).WithMessage("La descripción no debe exceder los 4000 caracteres.");
    }
}
