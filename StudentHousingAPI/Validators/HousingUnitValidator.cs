using Business.DTOs.Requests;
using FluentValidation;

namespace StudentHousingAPI.Validators;

public class HousingUnitCreateValidator : AbstractValidator<HousingUnitCreateRequest>
{
    public HousingUnitCreateValidator()
    {
        RuleFor(x => x.LandLordId)
            .NotEmpty().WithMessage("Landlord ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(200).WithMessage("Address must not exceed 200 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(50).WithMessage("City must not exceed 50 characters.");

        RuleFor(x => x.Area)
            .NotEmpty().WithMessage("Area is required.")
            .MaximumLength(100).WithMessage("Area must not exceed 100 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.GenderAllowed)
            .IsInEnum().WithMessage("Invalid gender value.");

        RuleFor(x => x.Rules)
            .MaximumLength(500).WithMessage("Rules must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Rules));

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.NumberOfRooms)
            .GreaterThan(0).WithMessage("Number of rooms must be greater than 0.");

        // UnitImageUrl: no length limit — the column is nvarchar(max) and
        // accepts both regular URLs and base64-encoded image strings.

        RuleFor(x => x.VideoUrl)
            .MaximumLength(500).WithMessage("Video URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.VideoUrl));
    }
}

public class HousingUnitUpdateValidator : AbstractValidator<HousingUnitUpdateRequest>
{
    public HousingUnitUpdateValidator()
    {
        RuleFor(x => x.HousingUnitId)
            .NotEmpty().WithMessage("Housing unit ID is required.");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Address)
            .MaximumLength(200).WithMessage("Address must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.City)
            .MaximumLength(50).WithMessage("City must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.Area)
            .MaximumLength(100).WithMessage("Area must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Area));

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.GenderAllowed)
            .IsInEnum().WithMessage("Invalid gender value.")
            .When(x => x.GenderAllowed.HasValue);

        RuleFor(x => x.Rules)
            .MaximumLength(500).WithMessage("Rules must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Rules));

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Location));

        RuleFor(x => x.NumberOfRooms)
            .GreaterThan(0).WithMessage("Number of rooms must be greater than 0.")
            .When(x => x.NumberOfRooms.HasValue);

        // UnitImageUrl: no length limit — nvarchar(max) column, accepts URLs and base64.

        RuleFor(x => x.VideoUrl)
            .MaximumLength(500).WithMessage("Video URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.VideoUrl));
    }
}

public class HousingUnitFilterValidator : AbstractValidator<HousingUnitFilterRequest>
{
    public HousingUnitFilterValidator()
    {
        RuleFor(x => x.City)
            .MaximumLength(50).WithMessage("City must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.Area)
            .MaximumLength(100).WithMessage("Area must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Area));

        RuleFor(x => x.MinPrice)
            .GreaterThan(0).WithMessage("Minimum price must be greater than 0.")
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThan(0).WithMessage("Maximum price must be greater than 0.")
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x.GenderAllowed)
            .IsInEnum().WithMessage("Invalid gender value.")
            .When(x => x.GenderAllowed.HasValue);

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
