# recipes_nestings

**Schema:** `public`  
**Table:** `recipes_nestings`  
**Entity:** `RecipeNesting`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `includes_recipe_id` | `uuid` | No | FK→recipes |  | |
| `recipe_id` | `uuid` | No | FK→recipes |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `IX_recipes_nestings_includes_recipe_id` | `includes_recipe_id` | No |
| `ix_recipes_nestings_tenant_id` | `tenant_id` | No |
| `ux_recipes_nestings_recipe_includes` | `recipe_id`, `includes_recipe_id` | Yes |

## Relationships

- **recipes_nestings** (`includes_recipe_id`) → **recipes** (`id`)
- **recipes_nestings** (`recipe_id`) → **recipes** (`id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

