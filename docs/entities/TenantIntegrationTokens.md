# TenantIntegrationTokens

**Schema:** `public`  
**Table:** `TenantIntegrationTokens`  
**Entity:** `TenantIntegrationToken`

## Primary Key

- `Id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `Id` | `TEXT` | No | PK |  | |
| `AccessToken` | `TEXT` | Yes |  |  | |
| `CreatedAt` | `TEXT` | No |  | CURRENT_TIMESTAMP | |
| `ExpiresAt` | `TEXT` | Yes |  |  | |
| `LastRefreshedAt` | `TEXT` | Yes |  |  | |
| `PluginId` | `TEXT` | No |  |  | |
| `RefreshToken` | `TEXT` | Yes |  |  | |
| `RequiresReauth` | `INTEGER` | No |  |  | |
| `TenantId` | `TEXT` | No |  |  | |
| `UpdatedAt` | `TEXT` | No |  | CURRENT_TIMESTAMP | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `IX_TenantIntegrationTokens_PluginId` | `PluginId` | No |
| `IX_TenantIntegrationTokens_TenantId` | `TenantId` | No |
| `IX_TenantIntegrationTokens_TenantId_PluginId` | `TenantId`, `PluginId` | Yes |

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

