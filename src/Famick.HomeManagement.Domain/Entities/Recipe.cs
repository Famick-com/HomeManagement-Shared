using Famick.HomeManagement.Domain.Interfaces;

namespace Famick.HomeManagement.Domain.Entities
{
    /// <summary>
    /// Represents a recipe (cooking instructions with ingredients).
    /// Example: "Chocolate Chip Cookies", "Spaghetti Carbonara", "Green Smoothie"
    /// </summary>
    public class Recipe : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// Tenant identifier for multi-tenancy isolation
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Recipe name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description or cooking instructions
        /// </summary>
        public string? Description { get; set; }

        // Navigation properties
        /// <summary>
        /// Ingredients/positions in this recipe
        /// </summary>
        public virtual ICollection<RecipePosition>? Positions { get; set; }

        /// <summary>
        /// Nested recipes that this recipe includes
        /// </summary>
        public virtual ICollection<RecipeNesting>? NestedRecipes { get; set; }

        /// <summary>
        /// Parent recipes that include this recipe
        /// </summary>
        public virtual ICollection<RecipeNesting>? ParentRecipes { get; set; }
    }
}
