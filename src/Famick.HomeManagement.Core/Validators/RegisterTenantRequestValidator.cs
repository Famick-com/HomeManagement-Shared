using Famick.HomeManagement.Core.DTOs.Authentication;
using FluentValidation;

namespace Famick.HomeManagement.Core.Validators;

/// <summary>
/// Validator for RegisterTenantRequest
/// Note: Subdomain uniqueness is checked in the service layer to maintain clean architecture
/// </summary>
public class RegisterTenantRequestValidator : AbstractValidator<RegisterTenantRequest>
{
    public RegisterTenantRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"\d").WithMessage("Password must contain at least one digit")
            .Matches(@"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]").WithMessage("Password must contain at least one special character");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Password and confirmation password must match");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters");

        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage("Organization name is required")
            .MaximumLength(200).WithMessage("Organization name must not exceed 200 characters");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomain is required")
            .MinimumLength(3).WithMessage("Subdomain must be at least 3 characters long")
            .MaximumLength(30).WithMessage("Subdomain must not exceed 30 characters")
            .Matches(@"^[a-z0-9-]+$").WithMessage("Subdomain must contain only lowercase letters, numbers, and hyphens")
            .Matches(@"^[a-z0-9].*[a-z0-9]$").WithMessage("Subdomain must start and end with a letter or number");
    }
}
