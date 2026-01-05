# user_permissions

**Schema:** `public`  
**Table:** `user_permissions`  
**Entity:** `UserPermission`

## Primary Key

- `Id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `Id` | `TEXT` | No | PK |  | |
| `CreatedAt` | `TEXT` | No |  | CURRENT_TIMESTAMP | |
| `PermissionId` | `TEXT` | No | FK→permissions |  | |
| `TenantId` | `TEXT` | No |  |  | |
| `UpdatedAt` | `TEXT` | Yes |  |  | |
| `UserId` | `TEXT` | No | FK→users |  | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `IX_user_permissions_PermissionId` | `PermissionId` | No |
| `IX_user_permissions_TenantId` | `TenantId` | No |
| `IX_user_permissions_UserId` | `UserId` | No |
| `IX_user_permissions_TenantId_UserId_PermissionId` | `TenantId`, `UserId`, `PermissionId` | Yes |

## Relationships

- **user_permissions** (`PermissionId`) → **permissions** (`Id`)
- **user_permissions** (`UserId`) → **users** (`Id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

