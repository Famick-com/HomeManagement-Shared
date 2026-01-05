# product_store_metadata

**Schema:** `public`  
**Table:** `product_store_metadata`  
**Entity:** `ProductStoreMetadata`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `aisle` | `character varying(50)` | Yes |  |  | |
| `availability_checked_at` | `timestamp with time zone` | Yes |  |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `department` | `character varying(100)` | Yes |  |  | |
| `external_product_id` | `character varying(100)` | Yes |  |  | |
| `in_stock` | `boolean` | Yes |  |  | |
| `last_known_price` | `numeric(10,2)` | Yes |  |  | |
| `price_unit` | `character varying(50)` | Yes |  |  | |
| `price_updated_at` | `timestamp with time zone` | Yes |  |  | |
| `product_id` | `uuid` | No | FK→products |  | |
| `ProductUrl` | `TEXT` | Yes |  |  | |
| `shelf` | `character varying(50)` | Yes |  |  | |
| `shopping_location_id` | `uuid` | No | FK→shopping_locations |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `IX_product_store_metadata_product_id` | `product_id` | No |
| `IX_product_store_metadata_shopping_location_id` | `shopping_location_id` | No |
| `ix_product_store_metadata_tenant_id` | `tenant_id` | No |
| `ix_product_store_metadata_tenant_product` | `tenant_id`, `product_id` | No |
| `ix_product_store_metadata_tenant_location` | `tenant_id`, `shopping_location_id` | No |
| `ux_product_store_metadata_product_location` | `tenant_id`, `product_id`, `shopping_location_id` | Yes |

## Relationships

- **product_store_metadata** (`product_id`) → **products** (`Id`)
- **product_store_metadata** (`shopping_location_id`) → **shopping_locations** (`id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

