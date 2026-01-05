# products

**Schema:** `public`  
**Table:** `products`  
**Entity:** `Product`

## Primary Key

- `Id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `Id` | `TEXT` | No | PK |  | |
| `CreatedAt` | `TEXT` | No |  | CURRENT_TIMESTAMP | |
| `DefaultBestBeforeDays` | `INTEGER` | No |  |  | |
| `Description` | `TEXT` | Yes |  |  | |
| `IsActive` | `INTEGER` | No |  |  | |
| `LocationId` | `TEXT` | No | FK→locations |  | |
| `MinStockAmount` | `TEXT` | No |  |  | |
| `Name` | `TEXT` | No |  |  | |
| `ProductGroupId` | `uuid` | Yes | FK→product_groups |  | |
| `QuantityUnitFactorPurchaseToStock` | `TEXT` | No |  |  | |
| `QuantityUnitIdPurchase` | `TEXT` | No | FK→quantity_units |  | |
| `QuantityUnitIdStock` | `TEXT` | No | FK→quantity_units |  | |
| `ServingSize` | `TEXT` | Yes |  |  | |
| `ServingUnit` | `TEXT` | Yes |  |  | |
| `ServingsPerContainer` | `TEXT` | Yes |  |  | |
| `ShoppingLocationId` | `uuid` | Yes | FK→shopping_locations |  | |
| `TenantId` | `TEXT` | No |  |  | |
| `UpdatedAt` | `TEXT` | Yes |  |  | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `IX_products_LocationId` | `LocationId` | No |
| `IX_products_ProductGroupId` | `ProductGroupId` | No |
| `IX_products_QuantityUnitIdPurchase` | `QuantityUnitIdPurchase` | No |
| `IX_products_QuantityUnitIdStock` | `QuantityUnitIdStock` | No |
| `IX_products_ShoppingLocationId` | `ShoppingLocationId` | No |
| `IX_products_TenantId` | `TenantId` | No |
| `IX_products_TenantId_Name` | `TenantId`, `Name` | No |

## Relationships

- **products** (`LocationId`) → **locations** (`Id`)
- **products** (`ProductGroupId`) → **product_groups** (`id`)
- **products** (`QuantityUnitIdPurchase`) → **quantity_units** (`Id`)
- **products** (`QuantityUnitIdStock`) → **quantity_units** (`Id`)
- **products** (`ShoppingLocationId`) → **shopping_locations** (`id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

