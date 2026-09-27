using FluentValidation;

namespace TaskFlow.Application.Validators;

public class CreateRoleValidator : AbstractValidator<DTOs.CreateRoleDto>
{
    public CreateRoleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del cargo es obligatorio.")
            .MaximumLength(30).WithMessage("El nombre del cargo no debe exceder los 30 caracteres.");
    }
}

public class UpdateRoleValidator : AbstractValidator<DTOs.UpdateRoleDto>
{
    public UpdateRoleValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del cargo es obligatorio.")
            .MaximumLength(30).WithMessage("El nombre del cargo no debe exceder los 30 caracteres.");
    }
}

public class SetPersonRolesValidator : AbstractValidator<DTOs.SetPersonRolesDto>
{
    public SetPersonRolesValidator()
    {
        RuleFor(x => x.RoleIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("La lista de cargos no puede tener duplicados.");
    }
}
