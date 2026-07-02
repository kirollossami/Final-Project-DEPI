using Business.DTOs.Requests;
using FluentValidation;

namespace StudentHousingAPI.Validators;

public class RoomCreateValidator : AbstractValidator<RoomCreateRequest>
{
    public RoomCreateValidator()
    {
        RuleFor(x => x.HousingUnitId)
            .NotEmpty().WithMessage("Housing unit ID is required.");

        RuleFor(x => x.RoomType)
            .IsInEnum().WithMessage("Invalid room type value.");

        RuleFor(x => x.RoomImageUrl)
            .MaximumLength(500).WithMessage("Room image URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.RoomImageUrl));

        RuleFor(x => x.NumberOfBeds)
            .GreaterThan(0).WithMessage("Number of beds must be greater than 0.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than 0.");
    }
}

public class RoomUpdateValidator : AbstractValidator<RoomUpdateRequest>
{
    public RoomUpdateValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required.");

        RuleFor(x => x.RoomType)
            .IsInEnum().WithMessage("Invalid room type value.")
            .When(x => x.RoomType.HasValue);

        RuleFor(x => x.RoomImageUrl)
            .MaximumLength(500).WithMessage("Room image URL must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.RoomImageUrl));

        RuleFor(x => x.NumberOfBeds)
            .GreaterThan(0).WithMessage("Number of beds must be greater than 0.")
            .When(x => x.NumberOfBeds.HasValue);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be greater than 0.")
            .When(x => x.Capacity.HasValue);
    }
}

public class RoomFilterValidator : AbstractValidator<RoomFilterRequest>
{
    public RoomFilterValidator()
    {
        RuleFor(x => x.RoomType)
            .IsInEnum().WithMessage("Invalid room type value.")
            .When(x => x.RoomType.HasValue);

        RuleFor(x => x.MinPrice)
            .GreaterThan(0).WithMessage("Minimum price must be greater than 0.")
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThan(0).WithMessage("Maximum price must be greater than 0.")
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
