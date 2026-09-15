using FluentValidation;

namespace TaskFlow.Application.Validators;

public class CreateTaskValidator : AbstractValidator<DTOs.CreateTaskDto>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Task title is required")
            .MaximumLength(300).WithMessage("Task title must not exceed 300 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters");

        RuleFor(x => x.ProjectId)
            .NotEmpty().WithMessage("Project ID is required");
    }
}
