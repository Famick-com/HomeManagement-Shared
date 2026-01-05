# users

**Schema:** `public`  
**Table:** `users`  
**Entity:** `User`

## Primary Key

- `Id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `Id` | `TEXT` | No | PK |  | |
| `CreatedAt` | `TEXT` | No |  | CURRENT_TIMESTAMP | |
| `Email` | `TEXT` | No |  |  | |
| `FirstName` | `TEXT` | No |  |  | |
| `IsActive` | `INTEGER` | No |  |  | |
| `LastLoginAt` | `TEXT` | Yes |  |  | |
| `LastName` | `TEXT` | No |  |  | |
| `PasswordHash` | `TEXT` | No |  |  | |
| `TenantId` | `TEXT` | No |  |  | |
| `UpdatedAt` | `TEXT` | Yes |  |  | |
| `Username` | `TEXT` | No |  |  | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `IX_users_TenantId` | `TenantId` | No |
| `IX_users_TenantId_Email` | `TenantId`, `Email` | Yes |
| `IX_users_TenantId_Username` | `TenantId`, `Username` | Yes |

## Relationships


## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

