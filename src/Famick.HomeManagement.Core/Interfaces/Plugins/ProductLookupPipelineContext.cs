using System.Text.RegularExpressions;

namespace Famick.HomeManagement.Core.Interfaces.Plugins;

/// <summary>
/// Type of product lookup search
/// </summary>
public enum ProductLookupSearchType
{
    Barcode,
    Name
}

/// <summary>
/// Context passed between plugins in the lookup pipeline.
/// Contains accumulated results and search parameters.
/// </summary>
public class ProductLookupPipelineContext
{
    private static readonly Regex DigitsOnly = new(@"[^0-9]", RegexOptions.Compiled);
    /// <summary>
    /// The original search query (barcode or name)
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// Type of search being performed
    /// </summary>
    public ProductLookupSearchType SearchType { get; }

    /// <summary>
    /// Maximum results requested
    /// </summary>
    public int MaxResults { get; }

    /// <summary>
    /// Accumulated results from previous plugins in the pipeline.
    /// Plugins can add new results or enrich existing ones.
    /// </summary>
    public List<ProductLookupResult> Results { get; }

    public ProductLookupPipelineContext(
        string query,
        ProductLookupSearchType searchType,
        int maxResults = 20)
    {
        Query = query;
        SearchType = searchType;
        MaxResults = maxResults;
        Results = new List<ProductLookupResult>();
    }

    /// <summary>
    /// Find an existing result that matches the given criteria.
    /// Matches by barcode (normalized) or by externalId+dataSource combination.
    /// </summary>
    public ProductLookupResult? FindMatchingResult(
        string? barcode = null,
        string? externalId = null,
        string? dataSource = null)
    {
        // Priority 1: Match by barcode (normalized for different formats)
        if (!string.IsNullOrEmpty(barcode))
        {
            var normalizedInput = NormalizeBarcode(barcode);
            var byBarcode = Results.FirstOrDefault(r =>
                !string.IsNullOrEmpty(r.Barcode) &&
                NormalizeBarcode(r.Barcode).Equals(normalizedInput, StringComparison.OrdinalIgnoreCase));
            if (byBarcode != null) return byBarcode;
        }

        // Priority 2: Match by externalId + dataSource (for same-source enrichment)
        if (!string.IsNullOrEmpty(externalId) && !string.IsNullOrEmpty(dataSource))
        {
            return Results.FirstOrDefault(r =>
                r.DataSources.TryGetValue(dataSource, out var id) &&
                id.Equals(externalId, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    /// <summary>
    /// Normalizes a barcode for comparison by stripping check digits and leading zeros.
    /// Handles UPC-A (12 digits), EAN-13 (13 digits), and various retailer formats.
    /// </summary>
    /// <remarks>
    /// Some systems (like Kroger) store barcodes without check digits and with extra padding.
    /// For example:
    /// - UPC-A with check: 761720051108 (12 digits)
    /// - Kroger format:    0076172005110 (13 digits, no check digit, padded)
    /// Both normalize to: 76172005110
    /// </remarks>
    public static string NormalizeBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return string.Empty;

        // Remove any non-digit characters
        var digits = DigitsOnly.Replace(barcode, "");
        if (string.IsNullOrEmpty(digits))
            return string.Empty;

        // Only strip check digit if it's actually a valid check digit
        // Some systems (like Kroger) store barcodes without check digits
        var withoutCheck = digits;

        if (digits.Length == 8 && HasValidCheckDigit(digits, isEan: true))
        {
            withoutCheck = digits[..7]; // EAN-8 without check
        }
        else if (digits.Length == 12 && HasValidCheckDigit(digits, isEan: false))
        {
            withoutCheck = digits[..11]; // UPC-A without check
        }
        else if (digits.Length == 13 && HasValidCheckDigit(digits, isEan: true))
        {
            withoutCheck = digits[..12]; // EAN-13 without check
        }
        else if (digits.Length == 14 && HasValidCheckDigit(digits, isEan: true))
        {
            withoutCheck = digits[..13]; // GTIN-14 without check
        }
        // For non-standard lengths or invalid check digits, keep as-is

        // Strip leading zeros for comparison
        var normalized = withoutCheck.TrimStart('0');

        // If all zeros, return "0"
        return string.IsNullOrEmpty(normalized) ? "0" : normalized;
    }

    /// <summary>
    /// Validates if the last digit of a barcode is a valid check digit.
    /// </summary>
    private static bool HasValidCheckDigit(string barcode, bool isEan)
    {
        if (barcode.Length < 2)
            return false;

        var checkDigit = barcode[^1] - '0';
        var data = barcode[..^1];
        var sum = 0;

        for (var i = 0; i < data.Length; i++)
        {
            var digit = data[i] - '0';
            if (isEan)
            {
                // EAN/GTIN: odd positions (0-indexed) × 1, even positions × 3
                sum += i % 2 == 0 ? digit : digit * 3;
            }
            else
            {
                // UPC-A: odd positions (0-indexed) × 3, even positions × 1
                sum += i % 2 == 0 ? digit * 3 : digit;
            }
        }

        var expectedCheck = (10 - (sum % 10)) % 10;
        return checkDigit == expectedCheck;
    }

    /// <summary>
    /// Find all results that match the given barcode (using normalized comparison).
    /// </summary>
    public IEnumerable<ProductLookupResult> FindResultsByBarcode(string barcode)
    {
        if (string.IsNullOrEmpty(barcode)) yield break;

        var normalizedInput = NormalizeBarcode(barcode);
        foreach (var result in Results)
        {
            if (!string.IsNullOrEmpty(result.Barcode) &&
                NormalizeBarcode(result.Barcode).Equals(normalizedInput, StringComparison.OrdinalIgnoreCase))
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Add a new result to the pipeline.
    /// </summary>
    public void AddResult(ProductLookupResult result)
    {
        Results.Add(result);
    }

    /// <summary>
    /// Add multiple results to the pipeline.
    /// </summary>
    public void AddResults(IEnumerable<ProductLookupResult> results)
    {
        Results.AddRange(results);
    }
}
