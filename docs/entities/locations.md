# locations

**Schema:** `public`  
**Table:** `locations`  
**Entity:** `Location`

## Primary Key

- `Id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `Id` | `TEXT` | No | PK |  | |
| `CreatedAt` | `TEXT` | No |  | CURRENT_TIMESTAMP | |
| `Description` | `TEXT` | Yes |  |  | |
| `IsActive` | `INTEGER` | No |  |  | |
| `Name` | `TEXT` | No |  |  | |
| `SortOrder` | `INTEGER` | No |  |  | |
| `TenantId` | `TEXT` | No |  |  | |
| `UpdatedAt` | `TEXT` | Yes |  |  | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `IX_locations_TenantId` | `TenantId` | No |
| `IX_locations_TenantId_Name` | `TenantId`, `Name` | Yes |

## Relationships


## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

