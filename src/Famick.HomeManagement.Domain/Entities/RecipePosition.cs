using Famick.HomeManagement.Domain.Interfaces;

namespace Famick.HomeManagement.Domain.Entities
{
    /// <summary>
    /// Represents an ingredient/position in a recipe.
    /// Links recipes to products with amounts and quantity units.
    /// Example: "2 cups of flour", "3 eggs", "1 tsp vanilla extract"
    /// </summary>
    public class RecipePosition : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// Tenant identifier for multi-tenancy isolation
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Recipe this ingredient belongs to
        /// </summary>
        public Guid RecipeId { get; set; }

        /// <summary>
        /// Product used as ingredient
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Amount/quantity of the ingredient
        /// </summary>
        public decimal Amount { get; set; } = 0;

        /// <summary>
        /// Optional note (preparation instructions, substitutions)
        /// Example: "finely chopped", "or use honey instead", "room temperature"
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Quantity unit for this ingredient (defaults to product's stock QU)
        /// </summary>
        public Guid? QuantityUnitId { get; set; }

        /// <summary>
        /// Only check if at least one unit of this product is in stock
        /// (don't check for exact amount when checking recipe fulfillment)
        /// </summary>
        public bool OnlyCheckSingleUnitInStock { get; set; } = false;

        /// <summary>
        /// Ingredient group for organizing recipe display
        /// Example: "Dry Ingredients", "Wet Ingredients", "Topping"
        /// </summary>
        public string? IngredientGroup { get; set; }

        /// <summary>
        /// Don't check stock fulfillment for this ingredient
        /// (useful for optional ingredients or those always assumed in stock like water, salt)
        /// </summary>
        public bool NotCheckStockFulfillment { get; set; } = false;

        // Navigation properties
        /// <summary>
        /// The recipe this ingredient belongs to
        /// </summary>
        public virtual Recipe? Recipe { get; set; }

        /// <summary>
        /// The product used as ingredient
        /// </summary>
        public virtual Product? Product { get; set; }

        /// <summary>
        /// The quantity unit for this ingredient
        /// </summary>
        public virtual QuantityUnit? QuantityUnit { get; set; }
    }
}
