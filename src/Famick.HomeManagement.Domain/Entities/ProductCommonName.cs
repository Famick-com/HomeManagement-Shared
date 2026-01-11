using Famick.HomeManagement.Domain.Interfaces;

namespace Famick.HomeManagement.Domain.Entities
{
    /// <summary>
    /// Represents a common/generic product name shared across branded products.
    /// Example: "Ground Cinnamon" for McCormick, Great Value, and other brands.
    /// Used for recipe matching and generic inventory searches.
    /// </summary>
    public class ProductCommonName : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// Tenant identifier for multi-tenancy isolation
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Common product name (unique per tenant)
        /// Example: "Ground Cinnamon", "All-Purpose Flour", "Whole Milk"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the common name
        /// </summary>
        public string? Description { get; set; }

        // Navigation properties
        /// <summary>
        /// Products with this common name
        /// </summary>
        public virtual ICollection<Product>? Products { get; set; }
    }
}
