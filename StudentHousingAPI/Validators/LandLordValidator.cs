using Business.DTOs.Requests;
using FluentValidation;

namespace StudentHousingAPI.Validators;

public class LandLordRegisterValidator : AbstractValidator<LandLordRegisterRequest>
{
    public LandLordRegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Confirm password must match password.");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^01[0125][0-9]{8}$").WithMessage("Invalid Egyptian phone number format.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.");

        RuleFor(x => x.NationalId)
            .Length(14).WithMessage("National ID must be exactly 14 digits.")
            .Matches(@"^[0-9]{14}$").WithMessage("National ID must contain only digits.");

        RuleFor(x => x.PropertyOwnerShipProof)
            .NotEmpty().WithMessage("Property ownership proof is required.")
            .MaximumLength(500).WithMessage("Property ownership proof must not exceed 500 characters.");
    }
}

public class LandLordUpdateValidator : AbstractValidator<UpdateLandLordRequest>
{
    public LandLordUpdateValidator()
    {
        RuleFor(x => x.LandLordId)
            .NotEmpty().WithMessage("Landlord ID is required.");

        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.CompanyName));

        RuleFor(x => x.PropertyOwnerShipProof)
            .MaximumLength(500).WithMessage("Property ownership proof must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.PropertyOwnerShipProof));
    }
}

public class LandLordFilterValidator : AbstractValidator<LandLordFilterRequest>
{
    public LandLordFilterValidator()
    {
        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.CompanyName));

        RuleFor(x => x.VerificationStatus)
            .MaximumLength(50).WithMessage("Verification status must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.VerificationStatus));

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
