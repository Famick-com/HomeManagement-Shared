# home_utilities

**Schema:** `public`  
**Table:** `home_utilities`  
**Entity:** `HomeUtility`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `account_number` | `character varying(100)` | Yes |  |  | |
| `company_name` | `character varying(255)` | Yes |  |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `home_id` | `uuid` | No | FK→homes |  | |
| `login_email` | `character varying(255)` | Yes |  |  | |
| `notes` | `text` | Yes |  |  | |
| `phone_number` | `character varying(50)` | Yes |  |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `utility_type` | `integer` | No |  |  | |
| `website` | `character varying(500)` | Yes |  |  | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `ix_home_utilities_home_id` | `home_id` | No |
| `ix_home_utilities_tenant_id` | `tenant_id` | No |
| `ux_home_utilities_home_type` | `home_id`, `utility_type` | Yes |

## Relationships

- **home_utilities** (`home_id`) → **homes** (`id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

