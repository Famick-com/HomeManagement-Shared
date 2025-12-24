using Famick.HomeManagement.Core.DTOs.ShoppingLists;
using FluentValidation;

namespace Famick.HomeManagement.Core.Validators.ShoppingLists;

public class AddShoppingListItemRequestValidator : AbstractValidator<AddShoppingListItemRequest>
{
    public AddShoppingListItemRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Note));

        // Either ProductId OR Note must be provided (or both)
        RuleFor(x => x)
            .Must(x => x.ProductId.HasValue || !string.IsNullOrWhiteSpace(x.Note))
            .WithMessage("Either ProductId or Note must be provided");
    }
}
