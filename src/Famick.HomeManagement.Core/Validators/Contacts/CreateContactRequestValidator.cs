using Famick.HomeManagement.Core.DTOs.Contacts;
using FluentValidation;

namespace Famick.HomeManagement.Core.Validators.Contacts;

public class CreateContactRequestValidator : AbstractValidator<CreateContactRequest>
{
    public CreateContactRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.MiddleName)
            .MaximumLength(100).WithMessage("Middle name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.MiddleName));

        RuleFor(x => x.PreferredName)
            .MaximumLength(100).WithMessage("Preferred name cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.PreferredName));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email address is not valid")
            .MaximumLength(255).WithMessage("Email cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Notes)
            .MaximumLength(5000).WithMessage("Notes cannot exceed 5000 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        // Birth date validation
        RuleFor(x => x.BirthYear)
            .InclusiveBetween(1800, 2100).WithMessage("Birth year must be between 1800 and 2100")
            .When(x => x.BirthYear.HasValue);

        RuleFor(x => x.BirthMonth)
            .InclusiveBetween(1, 12).WithMessage("Birth month must be between 1 and 12")
            .When(x => x.BirthMonth.HasValue);

        RuleFor(x => x.BirthDay)
            .InclusiveBetween(1, 31).WithMessage("Birth day must be between 1 and 31")
            .When(x => x.BirthDay.HasValue);

        // Death date validation
        RuleFor(x => x.DeathYear)
            .InclusiveBetween(1800, 2100).WithMessage("Death year must be between 1800 and 2100")
            .When(x => x.DeathYear.HasValue);

        RuleFor(x => x.DeathMonth)
            .InclusiveBetween(1, 12).WithMessage("Death month must be between 1 and 12")
            .When(x => x.DeathMonth.HasValue);

        RuleFor(x => x.DeathDay)
            .InclusiveBetween(1, 31).WithMessage("Death day must be between 1 and 31")
            .When(x => x.DeathDay.HasValue);
    }
}
