namespace Famick.HomeManagement.Core.DTOs.ShoppingLocations;

public class CreateShoppingLocationRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
