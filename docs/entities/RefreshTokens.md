# RefreshTokens

**Schema:** `public`  
**Table:** `RefreshTokens`  
**Entity:** `RefreshToken`

## Primary Key

- `Id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `Id` | `TEXT` | No | PK |  | |
| `CreatedAt` | `TEXT` | No |  | CURRENT_TIMESTAMP | |
| `DeviceInfo` | `TEXT` | No |  |  | |
| `ExpiresAt` | `TEXT` | No |  |  | |
| `IpAddress` | `TEXT` | No |  |  | |
| `IsRevoked` | `INTEGER` | No |  |  | |
| `RememberMe` | `INTEGER` | No |  |  | |
| `ReplacedByTokenId` | `TEXT` | Yes | FK→RefreshTokens |  | |
| `RevokedAt` | `TEXT` | Yes |  |  | |
| `TenantId` | `TEXT` | No |  |  | |
| `TokenHash` | `TEXT` | No |  |  | |
| `UpdatedAt` | `TEXT` | No |  | CURRENT_TIMESTAMP | |
| `UserId` | `TEXT` | No | FK→users |  | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `IX_RefreshTokens_ExpiresAt` | `ExpiresAt` | No |
| `IX_RefreshTokens_ReplacedByTokenId` | `ReplacedByTokenId` | No |
| `IX_RefreshTokens_TenantId` | `TenantId` | No |
| `IX_RefreshTokens_TokenHash` | `TokenHash` | No |
| `IX_RefreshTokens_UserId` | `UserId` | No |

## Relationships

- **RefreshTokens** (`ReplacedByTokenId`) → **RefreshTokens** (`Id`)
- **RefreshTokens** (`UserId`) → **users** (`Id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

