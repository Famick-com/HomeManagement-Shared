# product_nutrition

**Schema:** `public`  
**Table:** `product_nutrition`  
**Entity:** `ProductNutrition`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `added_sugars` | `numeric(10,3)` | Yes |  |  | |
| `brand_name` | `character varying(255)` | Yes |  |  | |
| `brand_owner` | `character varying(255)` | Yes |  |  | |
| `calcium` | `numeric(10,3)` | Yes |  |  | |
| `calories` | `numeric(10,2)` | Yes |  |  | |
| `cholesterol` | `numeric(10,3)` | Yes |  |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `data_source` | `character varying(50)` | No |  |  | |
| `dietary_fiber` | `numeric(10,3)` | Yes |  |  | |
| `external_id` | `character varying(100)` | Yes |  |  | |
| `folate` | `numeric(10,3)` | Yes |  |  | |
| `ingredients` | `text` | Yes |  |  | |
| `iron` | `numeric(10,3)` | Yes |  |  | |
| `last_updated_from_source` | `timestamp with time zone` | No |  |  | |
| `magnesium` | `numeric(10,3)` | Yes |  |  | |
| `niacin` | `numeric(10,3)` | Yes |  |  | |
| `phosphorus` | `numeric(10,3)` | Yes |  |  | |
| `potassium` | `numeric(10,3)` | Yes |  |  | |
| `product_id` | `uuid` | No | FK→products |  | |
| `protein` | `numeric(10,3)` | Yes |  |  | |
| `riboflavin` | `numeric(10,3)` | Yes |  |  | |
| `saturated_fat` | `numeric(10,3)` | Yes |  |  | |
| `serving_size` | `numeric(10,3)` | Yes |  |  | |
| `serving_size_description` | `character varying(255)` | Yes |  |  | |
| `serving_unit` | `character varying(50)` | Yes |  |  | |
| `ServingsPerContainer` | `TEXT` | Yes |  |  | |
| `sodium` | `numeric(10,3)` | Yes |  |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `thiamin` | `numeric(10,3)` | Yes |  |  | |
| `total_carbohydrates` | `numeric(10,3)` | Yes |  |  | |
| `total_fat` | `numeric(10,3)` | Yes |  |  | |
| `total_sugars` | `numeric(10,3)` | Yes |  |  | |
| `trans_fat` | `numeric(10,3)` | Yes |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `vitamin_a` | `numeric(10,3)` | Yes |  |  | |
| `vitamin_b12` | `numeric(10,3)` | Yes |  |  | |
| `vitamin_b6` | `numeric(10,3)` | Yes |  |  | |
| `vitamin_c` | `numeric(10,3)` | Yes |  |  | |
| `vitamin_d` | `numeric(10,3)` | Yes |  |  | |
| `vitamin_e` | `numeric(10,3)` | Yes |  |  | |
| `vitamin_k` | `numeric(10,3)` | Yes |  |  | |
| `zinc` | `numeric(10,3)` | Yes |  |  | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `ux_product_nutrition_product_id` | `product_id` | Yes |
| `ix_product_nutrition_tenant_id` | `tenant_id` | No |
| `ix_product_nutrition_data_source_external_id` | `data_source`, `external_id` | No |

## Relationships

- **product_nutrition** (`product_id`) → **products** (`Id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

