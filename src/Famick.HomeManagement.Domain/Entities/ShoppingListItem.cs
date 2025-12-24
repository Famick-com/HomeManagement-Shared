using Famick.HomeManagement.Domain.Interfaces;

namespace Famick.HomeManagement.Domain.Entities
{
    /// <summary>
    /// Represents an item in a shopping list (product to purchase with quantity).
    /// Links products to shopping lists with amount and optional notes.
    /// </summary>
    public class ShoppingListItem : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// Tenant identifier for multi-tenancy isolation
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Shopping list this item belongs to
        /// </summary>
        public Guid ShoppingListId { get; set; }

        /// <summary>
        /// Product to purchase (optional - can have note-only items)
        /// </summary>
        public Guid? ProductId { get; set; }

        /// <summary>
        /// Amount/quantity to purchase
        /// </summary>
        public decimal Amount { get; set; } = 0;

        /// <summary>
        /// Optional note (can be used for products not in the system or special instructions)
        /// Example: "Get the organic version", "Check for sales", "Generic bread"
        /// </summary>
        public string? Note { get; set; }

        // Navigation properties
        /// <summary>
        /// The shopping list this item belongs to
        /// </summary>
        public virtual ShoppingList? ShoppingList { get; set; }

        /// <summary>
        /// The product to purchase (if specified)
        /// </summary>
        public virtual Product? Product { get; set; }
    }
}
