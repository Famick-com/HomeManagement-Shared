using Famick.HomeManagement.Domain.Interfaces;

namespace Famick.HomeManagement.Domain.Entities
{
    /// <summary>
    /// Represents a shopping location (store, market, shop) where products can be purchased.
    /// Example: "Walmart", "Costco", "Local Farmer's Market", "Online - Amazon"
    /// </summary>
    public class ShoppingLocation : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// Tenant identifier for multi-tenancy isolation
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Shopping location name (unique per tenant)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Optional description (address, notes, opening hours, etc.)
        /// </summary>
        public string? Description { get; set; }

        // Navigation properties
        /// <summary>
        /// Products that have this as their default shopping location
        /// </summary>
        public virtual ICollection<Product>? Products { get; set; }
    }
}
