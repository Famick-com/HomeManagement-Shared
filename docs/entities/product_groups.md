# product_groups

**Schema:** `public`  
**Table:** `product_groups`  
**Entity:** `ProductGroup`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `description` | `text` | Yes |  |  | |
| `name` | `character varying(255)` | No |  |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `ix_product_groups_tenant_id` | `tenant_id` | No |
| `ux_product_groups_tenant_name` | `tenant_id`, `name` | Yes |

## Relationships


## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

