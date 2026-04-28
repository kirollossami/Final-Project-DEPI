using Business.DTOs.Requests;
using FluentValidation;

namespace StudentHousingAPI.Validators;

public class StudentRegisterValidator : AbstractValidator<StudentRegisterRequest>
{
    public StudentRegisterValidator()
    {
        // Inherited from RegisterRequest
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

        // Student-specific fields
        RuleFor(x => x.UserName)
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .Must(BeAtLeast16YearsOld).WithMessage("Student must be at least 16 years old.");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value.");

        RuleFor(x => x.Address)
            .MaximumLength(200).WithMessage("Address must not exceed 200 characters.");

        RuleFor(x => x.City)
            .MaximumLength(50).WithMessage("City must not exceed 50 characters.");

        RuleFor(x => x.PreferredArea)
            .MaximumLength(100).WithMessage("Preferred area must not exceed 100 characters.");

        RuleFor(x => x.NationalId)
            .Length(14).WithMessage("National ID must be exactly 14 digits.")
            .Matches(@"^[0-9]{14}$").WithMessage("National ID must contain only digits.")
            .When(x => !string.IsNullOrEmpty(x.NationalId));

        RuleFor(x => x.ProfileImage)
            .MaximumLength(500).WithMessage("Profile image URL must not exceed 500 characters.");
    }

    private bool BeAtLeast16YearsOld(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age >= 16;
    }
}

public class StudentUpdateValidator : AbstractValidator<StudentUpdateRequest>
{
    public StudentUpdateValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.FullName)
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.DateOfBirth)
            .Must(x => x.HasValue && BeAtLeast16YearsOld(x.Value)).WithMessage("Student must be at least 16 years old.")
            .When(x => x.DateOfBirth.HasValue);

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value.")
            .When(x => x.Gender.HasValue);

        RuleFor(x => x.Address)
            .MaximumLength(200).WithMessage("Address must not exceed 200 characters.");

        RuleFor(x => x.City)
            .MaximumLength(50).WithMessage("City must not exceed 50 characters.");

        RuleFor(x => x.PreferredArea)
            .MaximumLength(100).WithMessage("Preferred area must not exceed 100 characters.");

        RuleFor(x => x.NationalId)
            .Length(14).WithMessage("National ID must be exactly 14 digits.")
            .Matches(@"^[0-9]{14}$").WithMessage("National ID must contain only digits.")
            .When(x => !string.IsNullOrEmpty(x.NationalId));
    }

    private bool BeAtLeast16YearsOld(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age >= 16;
    }
}

public class StudentDeleteValidator : AbstractValidator<StudentDeleteRequest>
{
    public StudentDeleteValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");
    }
}

public class StudentFilterValidator : AbstractValidator<StudentFilterRequest>
{
    public StudentFilterValidator()
    {
        RuleFor(x => x.City)
            .MaximumLength(50).WithMessage("City must not exceed 50 characters.");

        RuleFor(x => x.PreferredArea)
            .MaximumLength(100).WithMessage("Preferred area must not exceed 100 characters.");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value.")
            .When(x => x.Gender.HasValue);

        RuleFor(x => x.DateOfBirthFrom)
            .Must(x => x.HasValue && BeAtLeast16YearsOld(x.Value)).WithMessage("Date of birthFrom must be at least 16 years old.")
            .When(x => x.DateOfBirthFrom.HasValue);

        RuleFor(x => x.DateOfBirthTo)
            .Must(x => x.HasValue && BeAtLeast16YearsOld(x.Value)).WithMessage("Date of birthTo must be at least 16 years old.")
            .When(x => x.DateOfBirthTo.HasValue);

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }

    private bool BeAtLeast16YearsOld(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age >= 16;
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(6).WithMessage("New password must be at least 6 characters long.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Confirm new password must match new password.");
    }
}
