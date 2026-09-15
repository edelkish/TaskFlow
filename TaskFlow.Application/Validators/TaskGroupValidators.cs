using FluentValidation;

namespace TaskFlow.Application.Validators;

public class CreateTaskGroupValidator : AbstractValidator<DTOs.CreateTaskGroupDto>
{
    public CreateTaskGroupValidator()
    {
        RuleFor(x => x.PeriodId)
            .NotEmpty().WithMessage("El periodo es obligatorio.");

        RuleFor(x => x.ProjectId)
            .NotEmpty().When(x => x.DevPersonId.HasValue)
            .WithMessage("Si hay desarrollador asignado, el proyecto es obligatorio.");

        RuleFor(x => x.ProjectId)
            .Null().When(x => !x.DevPersonId.HasValue && !x.QaPersonId.HasValue);

        RuleFor(x => x).Must(x => x.DevPersonId.HasValue || x.QaPersonId.HasValue)
            .WithMessage("El grupo requiere al menos un desarrollador o un responsable de QA.");
    }
}

public class UpdateTaskGroupValidator : AbstractValidator<DTOs.UpdateTaskGroupDto>
{
    public UpdateTaskGroupValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty().When(x => x.DevPersonId.HasValue)
            .WithMessage("Si hay desarrollador asignado, el proyecto es obligatorio.");

        RuleFor(x => x).Must(x =>
                x.DevPersonId.HasValue || x.QaPersonId.HasValue || !x.DevPersonId.HasValue && x.ProjectId.HasValue)
            .WithMessage("El grupo requiere al menos un desarrollador o un responsable de QA.");
    }
}