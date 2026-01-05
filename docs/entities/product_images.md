# product_images

**Schema:** `public`  
**Table:** `product_images`  
**Entity:** `ProductImage`

## Primary Key

- `Id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `Id` | `TEXT` | No | PK |  | |
| `ContentType` | `TEXT` | No |  |  | |
| `CreatedAt` | `TEXT` | No |  | CURRENT_TIMESTAMP | |
| `ExternalSource` | `TEXT` | Yes |  |  | |
| `ExternalThumbnailUrl` | `TEXT` | Yes |  |  | |
| `ExternalUrl` | `TEXT` | Yes |  |  | |
| `FileName` | `TEXT` | No |  |  | |
| `FileSize` | `INTEGER` | No |  |  | |
| `IsPrimary` | `INTEGER` | No |  |  | |
| `OriginalFileName` | `TEXT` | No |  |  | |
| `ProductId` | `TEXT` | No | FK→products |  | |
| `SortOrder` | `INTEGER` | No |  |  | |
| `TenantId` | `TEXT` | No |  |  | |
| `UpdatedAt` | `TEXT` | Yes |  |  | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `IX_product_images_ProductId` | `ProductId` | No |
| `IX_product_images_TenantId` | `TenantId` | No |
| `IX_product_images_TenantId_ProductId` | `TenantId`, `ProductId` | No |

## Relationships

- **product_images** (`ProductId`) → **products** (`Id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

