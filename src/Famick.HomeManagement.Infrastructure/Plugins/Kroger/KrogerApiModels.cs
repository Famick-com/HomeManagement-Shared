using System.Text.Json.Serialization;

namespace Famick.HomeManagement.Infrastructure.Plugins.Kroger;

#region OAuth Response Models

public class KrogerTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;
}

#endregion

#region Location Response Models

public class KrogerLocationsResponse
{
    [JsonPropertyName("data")]
    public List<KrogerLocation> Data { get; set; } = new();

    [JsonPropertyName("meta")]
    public KrogerMeta? Meta { get; set; }
}

public class KrogerLocation
{
    [JsonPropertyName("locationId")]
    public string LocationId { get; set; } = string.Empty;

    [JsonPropertyName("chain")]
    public string Chain { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public KrogerAddress? Address { get; set; }

    [JsonPropertyName("geolocation")]
    public KrogerGeoLocation? GeoLocation { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("departments")]
    public List<KrogerDepartment>? Departments { get; set; }

    [JsonPropertyName("hours")]
    public KrogerHours? Hours { get; set; }
}

public class KrogerAddress
{
    [JsonPropertyName("addressLine1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("zipCode")]
    public string ZipCode { get; set; } = string.Empty;

    [JsonPropertyName("county")]
    public string? County { get; set; }
}

public class KrogerGeoLocation
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("latLng")]
    public string? LatLng { get; set; }
}

public class KrogerDepartment
{
    [JsonPropertyName("departmentId")]
    public string DepartmentId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("hours")]
    public KrogerHours? Hours { get; set; }
}

public class KrogerHours
{
    [JsonPropertyName("open24")]
    public bool Open24 { get; set; }

    [JsonPropertyName("monday")]
    public KrogerDayHours? Monday { get; set; }

    [JsonPropertyName("tuesday")]
    public KrogerDayHours? Tuesday { get; set; }

    [JsonPropertyName("wednesday")]
    public KrogerDayHours? Wednesday { get; set; }

    [JsonPropertyName("thursday")]
    public KrogerDayHours? Thursday { get; set; }

    [JsonPropertyName("friday")]
    public KrogerDayHours? Friday { get; set; }

    [JsonPropertyName("saturday")]
    public KrogerDayHours? Saturday { get; set; }

    [JsonPropertyName("sunday")]
    public KrogerDayHours? Sunday { get; set; }
}

public class KrogerDayHours
{
    [JsonPropertyName("open")]
    public string? Open { get; set; }

    [JsonPropertyName("close")]
    public string? Close { get; set; }

    [JsonPropertyName("open24")]
    public bool Open24 { get; set; }
}

#endregion

#region Product Response Models

public class KrogerProductsResponse
{
    [JsonPropertyName("data")]
    public List<KrogerProduct> Data { get; set; } = new();

    [JsonPropertyName("meta")]
    public KrogerMeta? Meta { get; set; }
}

public class KrogerProduct
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;

    [JsonPropertyName("upc")]
    public string? Upc { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; set; }

    [JsonPropertyName("countryOrigin")]
    public string? CountryOrigin { get; set; }

    [JsonPropertyName("images")]
    public List<KrogerImage>? Images { get; set; }

    [JsonPropertyName("items")]
    public List<KrogerItem>? Items { get; set; }

    [JsonPropertyName("aisleLocations")]
    public List<KrogerAisleLocation>? AisleLocations { get; set; }

    [JsonPropertyName("temperature")]
    public KrogerTemperature? Temperature { get; set; }
}

public class KrogerImage
{
    [JsonPropertyName("perspective")]
    public string Perspective { get; set; } = string.Empty;

    [JsonPropertyName("featured")]
    public bool Featured { get; set; }

    [JsonPropertyName("sizes")]
    public List<KrogerImageSize>? Sizes { get; set; }
}

public class KrogerImageSize
{
    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class KrogerItem
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("favorite")]
    public bool Favorite { get; set; }

    [JsonPropertyName("fulfillment")]
    public KrogerFulfillment? Fulfillment { get; set; }

    [JsonPropertyName("price")]
    public KrogerPrice? Price { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }

    [JsonPropertyName("soldBy")]
    public string? SoldBy { get; set; }
}

public class KrogerFulfillment
{
    [JsonPropertyName("curbside")]
    public bool Curbside { get; set; }

    [JsonPropertyName("delivery")]
    public bool Delivery { get; set; }

    [JsonPropertyName("inStore")]
    public bool InStore { get; set; }

    [JsonPropertyName("shipToHome")]
    public bool ShipToHome { get; set; }
}

public class KrogerPrice
{
    [JsonPropertyName("regular")]
    public decimal? Regular { get; set; }

    [JsonPropertyName("promo")]
    public decimal? Promo { get; set; }

    [JsonPropertyName("regularPerUnitEstimate")]
    public decimal? RegularPerUnitEstimate { get; set; }

    [JsonPropertyName("promoPerUnitEstimate")]
    public decimal? PromoPerUnitEstimate { get; set; }
}

public class KrogerAisleLocation
{
    [JsonPropertyName("bayNumber")]
    public string? BayNumber { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("numberOfFacings")]
    public string? NumberOfFacings { get; set; }

    [JsonPropertyName("sequenceNumber")]
    public string? SequenceNumber { get; set; }

    [JsonPropertyName("side")]
    public string? Side { get; set; }

    [JsonPropertyName("shelfNumber")]
    public string? ShelfNumber { get; set; }

    [JsonPropertyName("shelfPositionInBay")]
    public string? ShelfPositionInBay { get; set; }
}

public class KrogerTemperature
{
    [JsonPropertyName("indicator")]
    public string? Indicator { get; set; }

    [JsonPropertyName("heatSensitive")]
    public bool HeatSensitive { get; set; }
}

public class KrogerMeta
{
    [JsonPropertyName("pagination")]
    public KrogerPagination? Pagination { get; set; }
}

public class KrogerPagination
{
    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

#endregion
