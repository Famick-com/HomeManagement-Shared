# product_barcodes

**Schema:** `public`  
**Table:** `product_barcodes`  
**Entity:** `ProductBarcode`

## Primary Key

- `Id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `Id` | `TEXT` | No | PK |  | |
| `Barcode` | `TEXT` | No |  |  | |
| `CreatedAt` | `TEXT` | No |  | CURRENT_TIMESTAMP | |
| `Note` | `TEXT` | Yes |  |  | |
| `ProductId` | `TEXT` | No | FK→products |  | |
| `TenantId` | `TEXT` | No |  |  | |
| `UpdatedAt` | `TEXT` | Yes |  |  | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `IX_product_barcodes_ProductId` | `ProductId` | No |
| `IX_product_barcodes_TenantId` | `TenantId` | No |
| `IX_product_barcodes_TenantId_Barcode` | `TenantId`, `Barcode` | Yes |

## Relationships

- **product_barcodes** (`ProductId`) → **products** (`Id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

