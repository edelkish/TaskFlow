using FluentValidation;

namespace TaskFlow.Application.Validators;

public class CreateProjectValidator : AbstractValidator<DTOs.CreateProjectDto>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required")
            .MaximumLength(200).WithMessage("Project name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Start date is required");
    }
}
