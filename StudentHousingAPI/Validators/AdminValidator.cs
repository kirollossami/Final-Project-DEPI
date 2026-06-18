using Business.DTOs.Requests;
using Domain.Enums;
using FluentValidation;

namespace StudentHousingAPI.Validators;

public class ReviewVerificationValidator : AbstractValidator<ReviewVerificationRequest>
{
    public ReviewVerificationValidator()
    {
        RuleFor(x => x.NewStatus)
            .Must(status => status == UniversityVerificationStatus.Approved || status == UniversityVerificationStatus.Rejected)
            .WithMessage("Verification status can only be set to Approved or Rejected.");
    }
}

public class AdminUserFilterValidator : AbstractValidator<AdminUserFilterRequest>
{
    public AdminUserFilterValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}
