using Famick.HomeManagement.Core.DTOs.ProductCommonNames;
using FluentValidation;

namespace Famick.HomeManagement.Core.Validators.ProductCommonNames;

public class UpdateProductCommonNameRequestValidator : AbstractValidator<UpdateProductCommonNameRequest>
{
    public UpdateProductCommonNameRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Common name is required")
            .MaximumLength(300).WithMessage("Common name cannot exceed 300 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
