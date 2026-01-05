# stock

**Schema:** `public`  
**Table:** `stock`  
**Entity:** `StockEntry`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `amount` | `numeric(18,4)` | No |  |  | |
| `best_before_date` | `timestamp with time zone` | Yes |  |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `location_id` | `uuid` | Yes | FK→locations |  | |
| `note` | `text` | Yes |  |  | |
| `open` | `boolean` | No |  |  | |
| `open_tracking_mode` | `integer` | Yes |  |  | |
| `opened_date` | `timestamp with time zone` | Yes |  |  | |
| `original_amount` | `numeric(18,4)` | Yes |  |  | |
| `price` | `numeric(18,4)` | Yes |  |  | |
| `product_id` | `uuid` | No | FK→products |  | |
| `purchased_date` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `shopping_location_id` | `uuid` | Yes |  |  | |
| `stock_id` | `character varying(100)` | No |  |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `IX_stock_location_id` | `location_id` | No |
| `ix_stock_stock_id` | `stock_id` | No |
| `ix_stock_tenant_id` | `tenant_id` | No |
| `ux_stock_tenant_stock_id` | `tenant_id`, `stock_id` | Yes |
| `ix_stock_performance1` | `product_id`, `open`, `best_before_date`, `amount` | No |

## Relationships

- **stock** (`location_id`) → **locations** (`Id`)
- **stock** (`product_id`) → **products** (`Id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

