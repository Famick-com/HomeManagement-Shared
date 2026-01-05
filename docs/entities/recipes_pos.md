# recipes_pos

**Schema:** `public`  
**Table:** `recipes_pos`  
**Entity:** `RecipePosition`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `amount` | `numeric(18,4)` | No |  |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `ingredient_group` | `character varying(255)` | Yes |  |  | |
| `not_check_stock_fulfillment` | `smallint` | No |  |  | |
| `note` | `text` | Yes |  |  | |
| `only_check_single_unit_in_stock` | `smallint` | No |  |  | |
| `product_id` | `uuid` | No | FK→products |  | |
| `qu_id` | `uuid` | Yes | FK→quantity_units |  | |
| `recipe_id` | `uuid` | No | FK→recipes |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `ix_recipes_pos_product_id` | `product_id` | No |
| `IX_recipes_pos_qu_id` | `qu_id` | No |
| `ix_recipes_pos_recipe_id` | `recipe_id` | No |
| `ix_recipes_pos_tenant_id` | `tenant_id` | No |

## Relationships

- **recipes_pos** (`product_id`) → **products** (`Id`)
- **recipes_pos** (`qu_id`) → **quantity_units** (`Id`)
- **recipes_pos** (`recipe_id`) → **recipes** (`id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

