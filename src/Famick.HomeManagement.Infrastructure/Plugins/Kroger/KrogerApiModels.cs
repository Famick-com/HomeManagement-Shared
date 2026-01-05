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

#region Cart Response Models

public class KrogerCartResponse
{
    [JsonPropertyName("data")]
    public KrogerCartData? Data { get; set; }
}

public class KrogerCartData
{
    [JsonPropertyName("items")]
    public List<KrogerCartItem>? Items { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal? Subtotal { get; set; }

    [JsonPropertyName("cartId")]
    public string? CartId { get; set; }
}

public class KrogerCartItem
{
    [JsonPropertyName("upc")]
    public string? Upc { get; set; }

    [JsonPropertyName("productId")]
    public string? ProductId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    [JsonPropertyName("imageUrl")]
    public string? ImageUrl { get; set; }
}

public class KrogerCartUpdateRequest
{
    [JsonPropertyName("items")]
    public List<KrogerCartItemUpdate> Items { get; set; } = new();
}

public class KrogerCartItemUpdate
{
    [JsonPropertyName("upc")]
    public string Upc { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

public class ProductModel
{
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = default!;

    [JsonPropertyName("productPageURI")]
    public string ProductPageUri { get; set; } = default!;

    [JsonPropertyName("aliasProductIds")]
    public List<string> AliasProductIds { get; set; } = [];

    [JsonPropertyName("aisleLocations")]
    public List<AisleLocation> AisleLocations { get; set; } = [];

    [JsonPropertyName("brand")]
    public string Brand { get; set; } = default!;

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = [];

    [JsonPropertyName("countryOrigin")]
    public string CountryOrigin { get; set; } = default!;

    [JsonPropertyName("description")]
    public string Description { get; set; } = default!;

    [JsonPropertyName("alcohol")]
    public bool Alcohol { get; set; }

    [JsonPropertyName("alcoholProof")]
    public int AlcoholProof { get; set; }

    [JsonPropertyName("ageRestriction")]
    public bool AgeRestriction { get; set; }

    [JsonPropertyName("snapEligible")]
    public bool SnapEligible { get; set; }

    [JsonPropertyName("manufacturerDeclarations")]
    public List<string> ManufacturerDeclarations { get; set; } = [];

    [JsonPropertyName("sweeteningMethods")]
    public CodeName SweeteningMethods { get; set; } = default!;

    [JsonPropertyName("allergens")]
    public List<Allergen> Allergens { get; set; } = [];

    [JsonPropertyName("allergensDescription")]
    public string AllergensDescription { get; set; } = default!;

    [JsonPropertyName("certifiedForPassover")]
    public bool CertifiedForPassover { get; set; }

    [JsonPropertyName("hypoallergenic")]
    public bool Hypoallergenic { get; set; }

    [JsonPropertyName("nonGmo")]
    public bool NonGmo { get; set; }

    [JsonPropertyName("nonGmoClaimName")]
    public string NonGmoClaimName { get; set; } = default!;

    [JsonPropertyName("organicClaimName")]
    public string OrganicClaimName { get; set; } = default!;

    [JsonPropertyName("receiptDescription")]
    public string ReceiptDescription { get; set; } = default!;

    [JsonPropertyName("warnings")]
    public string Warnings { get; set; } = default!;

    [JsonPropertyName("retstrictions")]
    public Restrictions Restrictions { get; set; } = default!;

    [JsonPropertyName("items")]
    public List<Item> Items { get; set; } = [];

    [JsonPropertyName("itemInformation")]
    public ItemInformation ItemInformation { get; set; } = default!;

    [JsonPropertyName("temperature")]
    public Temperature Temperature { get; set; } = default!;

    [JsonPropertyName("images")]
    public List<Image> Images { get; set; } = [];

    [JsonPropertyName("upc")]
    public string Upc { get; set; } = default!;

    [JsonPropertyName("ratingsAndReviews")]
    public RatingsAndReviews RatingsAndReviews { get; set; } = default!;

    [JsonPropertyName("nutritionInformation")]
    public NutritionInformation NutritionInformation { get; set; } = default!;    
}


public class AisleLocation
{
    public string BayNumber { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Number { get; set; } = default!;
    public string NumberOfFacings { get; set; } = default!;
    public string SequenceNumber { get; set; } = default!;
    public string Side { get; set; } = default!;
    public string ShelfNumber { get; set; } = default!;
    public string ShelfPositionInBay { get; set; } = default!;
}

public class CodeName
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}

public class Allergen
{
    public string LevelOfContainmentName { get; set; } = default!;
    public string Name { get; set; } = default!;
}

public class Restrictions
{
    public int MaximumOrderQuantity { get; set; }
    public int MinimumOrderQuantity { get; set; }
    public List<string> PostalCode { get; set; } = [];
    public bool Shippable { get; set; }
    public List<string> StateCodes { get; set; } = [];
}

public class Item
{
    public string ItemId { get; set; } = default!;
    public Inventory Inventory { get; set; } = default!;
    public bool Favorite { get; set; }
    public Fulfillment Fulfillment { get; set; } = default!;
    public Price Price { get; set; } = default!;
    public Price NationalPrice { get; set; } = default!;
    public string Size { get; set; } = default!;
    public string SoldBy { get; set; } = default!;
}

public class Inventory
{
    public string StockLevel { get; set; } = default!;
}

public class Fulfillment
{
    public bool Curbside { get; set; }
    public bool Delivery { get; set; }
    public bool Instore { get; set; }
    public bool Shiptohome { get; set; }
}

public class Price
{
    public decimal Regular { get; set; }
    public decimal Promo { get; set; }
    public decimal RegularPerUnitEstimate { get; set; }
    public decimal PromoPerUnitEstimate { get; set; }
    public ZonedDate ExpirationDate { get; set; } = default!;
    public ZonedDate EffectiveDate { get; set; } = default!;
}

public class ZonedDate
{
    public DateTime Value { get; set; }
    public string Timezone { get; set; } = default!;
}

public class ItemInformation
{
    public string Depth { get; set; } = default!;
    public string Height { get; set; } = default!;
    public string Width { get; set; } = default!;
    public string GrossWeight { get; set; } = default!;
    public string NetWeight { get; set; } = default!;
    public string AverageWeightPerUnit { get; set; } = default!;
}

public class Temperature
{
    public string Indicator { get; set; } = default!;
    public bool HeatSensitive { get; set; }
}

public class Image
{
    public string Id { get; set; } = default!;
    public string Perspective { get; set; } = default!;

    [JsonPropertyName("default")]
    public bool IsDefault { get; set; }

    public List<ImageSize> Sizes { get; set; } = [];
}

public class ImageSize
{
    public string Id { get; set; } = default!;
    public string Size { get; set; } = default!;
    public string Url { get; set; } = default!;
}

public class RatingsAndReviews
{
    public double AverageOverallRating { get; set; }
    public int TotalReviewCount { get; set; }
}

public class NutritionInformation
{
    public string IngredientStatement { get; set; } = default!;
    public string DailyValueIntakeReference { get; set; } = default!;
    public ServingSize ServingSize { get; set; } = default!;
    public List<Nutrient> Nutrients { get; set; } = [];
    public CodeName PreparationState { get; set; } = default!;
    public ServingsPerPackage ServingsPerPackage { get; set; } = default!;
    public string NutritionalRating { get; set; } = default!;
}

public class ServingSize
{
    public string Description { get; set; } = default!;
    public int Quantity { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = default!;
}

public class Nutrient
{
    public string Code { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public int PercentDailyIntake { get; set; }
    public int Quantity { get; set; }
    public CodeName Precision { get; set; } = default!;
    public UnitOfMeasure UnitOfMeasure { get; set; } = default!;
}

public class UnitOfMeasure
{
    public string Abbreviation { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
}

public class ServingsPerPackage
{
    public string Description { get; set; } = default!;
    public int Value { get; set; }
}
#endregion
