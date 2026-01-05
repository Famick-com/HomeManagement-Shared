# recipes

**Schema:** `public`  
**Table:** `recipes`  
**Entity:** `Recipe`

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
| `ix_recipes_tenant_id` | `tenant_id` | No |
| `ix_recipes_tenant_name` | `tenant_id`, `name` | No |

## Relationships


## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

