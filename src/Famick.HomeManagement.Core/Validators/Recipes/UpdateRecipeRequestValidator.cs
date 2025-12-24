using Famick.HomeManagement.Core.DTOs.Recipes;
using FluentValidation;

namespace Famick.HomeManagement.Core.Validators.Recipes;

public class UpdateRecipeRequestValidator : AbstractValidator<UpdateRecipeRequest>
{
    public UpdateRecipeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Recipe name is required")
            .MaximumLength(200).WithMessage("Recipe name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
