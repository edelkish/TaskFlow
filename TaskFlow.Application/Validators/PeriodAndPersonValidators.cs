using FluentValidation;

namespace TaskFlow.Application.Validators;

public class CreatePeriodValidator : AbstractValidator<DTOs.CreatePeriodDto>
{
    public CreatePeriodValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("El año debe estar entre 2000 y 2100.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("El mes debe estar entre 1 y 12.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del periodo es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no debe exceder los 100 caracteres.");
    }
}

public class CreatePersonValidator : AbstractValidator<DTOs.CreatePersonDto>
{
    public CreatePersonValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no debe exceder los 200 caracteres.");

        RuleFor(x => x.UserId)
            .MaximumLength(450).WithMessage("El identificador de usuario no debe exceder los 450 caracteres.");
    }
}

public class UpdatePersonValidator : AbstractValidator<DTOs.UpdatePersonDto>
{
    public UpdatePersonValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(200).WithMessage("El nombre no debe exceder los 200 caracteres.");

        RuleFor(x => x.UserId)
            .MaximumLength(450).WithMessage("El identificador de usuario no debe exceder los 450 caracteres.");
    }
}