namespace Famick.HomeManagement.Core.DTOs.Equipment;

/// <summary>
/// Request to update an existing equipment item
/// </summary>
public class UpdateEquipmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? ModelNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string? PurchaseLocation { get; set; }
    public DateTime? WarrantyExpirationDate { get; set; }
    public string? WarrantyContactInfo { get; set; }
    public string? Notes { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ParentEquipmentId { get; set; }
}
