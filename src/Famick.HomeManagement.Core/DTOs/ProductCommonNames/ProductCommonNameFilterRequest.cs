namespace Famick.HomeManagement.Core.DTOs.ProductCommonNames;

/// <summary>
/// Filter/search parameters for listing product common names
/// </summary>
public class ProductCommonNameFilterRequest
{
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool Descending { get; set; }
}
