# shopping_locations

**Schema:** `public`  
**Table:** `shopping_locations`  
**Entity:** `ShoppingLocation`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `description` | `text` | Yes |  |  | |
| `external_chain_id` | `character varying(100)` | Yes |  |  | |
| `external_location_id` | `character varying(100)` | Yes |  |  | |
| `integration_type` | `character varying(50)` | Yes |  |  | |
| `latitude` | `double precision` | Yes |  |  | |
| `longitude` | `double precision` | Yes |  |  | |
| `name` | `character varying(255)` | No |  |  | |
| `oauth_access_token` | `text` | Yes |  |  | |
| `oauth_refresh_token` | `text` | Yes |  |  | |
| `oauth_token_expires_at` | `timestamp with time zone` | Yes |  |  | |
| `store_address` | `character varying(500)` | Yes |  |  | |
| `store_phone` | `character varying(50)` | Yes |  |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `ix_shopping_locations_tenant_id` | `tenant_id` | No |
| `ix_shopping_locations_tenant_integration_type` | `tenant_id`, `integration_type` | No |
| `ux_shopping_locations_tenant_name` | `tenant_id`, `name` | Yes |

## Relationships


## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

