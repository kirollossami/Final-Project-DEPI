using Business.DTOs.Requests;
using Domain.Enums;
using FluentValidation;

namespace StudentHousingAPI.Validators;

public class ComplaintCreateValidator : AbstractValidator<ComplaintCreateRequest>
{
    public ComplaintCreateValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.HousingUnitId)
            .NotEmpty().WithMessage("Housing unit ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
    }
}

public class ComplaintUpdateValidator : AbstractValidator<ComplaintUpdateRequest>
{
    public ComplaintUpdateValidator()
    {
        RuleFor(x => x.ComplaintId)
            .NotEmpty().WithMessage("Complaint ID is required.");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid complaint status value.")
            .When(x => x.Status.HasValue);
    }
}

public class ComplaintFilterValidator : AbstractValidator<ComplaintFilterRequest>
{
    public ComplaintFilterValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid complaint status value.")
            .When(x => x.Status.HasValue);

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
