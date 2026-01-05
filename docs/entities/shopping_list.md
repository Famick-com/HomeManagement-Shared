# shopping_list

**Schema:** `public`  
**Table:** `shopping_list`  
**Entity:** `ShoppingListItem`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `amount` | `numeric(18,4)` | No |  |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `note` | `text` | Yes |  |  | |
| `product_id` | `uuid` | Yes | FK→products |  | |
| `shopping_list_id` | `uuid` | No | FK→shopping_lists |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `ix_shopping_list_product_id` | `product_id` | No |
| `ix_shopping_list_shopping_list_id` | `shopping_list_id` | No |
| `ix_shopping_list_tenant_id` | `tenant_id` | No |

## Relationships

- **shopping_list** (`product_id`) → **products** (`Id`)
- **shopping_list** (`shopping_list_id`) → **shopping_lists** (`id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

