# stock_log

**Schema:** `public`  
**Table:** `stock_log`  
**Entity:** `StockLog`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `amount` | `numeric(18,4)` | No |  |  | |
| `best_before_date` | `timestamp with time zone` | Yes |  |  | |
| `correlation_id` | `character varying(100)` | Yes |  |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `location_id` | `uuid` | Yes | FK→locations |  | |
| `note` | `text` | Yes |  |  | |
| `opened_date` | `timestamp with time zone` | Yes |  |  | |
| `price` | `numeric(18,4)` | Yes |  |  | |
| `product_id` | `uuid` | No | FK→products |  | |
| `purchased_date` | `timestamp with time zone` | Yes |  |  | |
| `recipe_id` | `uuid` | Yes |  |  | |
| `shopping_location_id` | `uuid` | Yes |  |  | |
| `spoiled` | `integer` | No |  |  | |
| `stock_id` | `character varying(100)` | No |  |  | |
| `stock_row_id` | `uuid` | Yes | FK→stock |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `transaction_id` | `character varying(100)` | Yes |  |  | |
| `transaction_type` | `character varying(50)` | No |  |  | |
| `undone` | `boolean` | No |  |  | |
| `undone_timestamp` | `timestamp with time zone` | Yes |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `used_date` | `timestamp with time zone` | Yes |  |  | |
| `user_id` | `uuid` | No | FK→users |  | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `ix_stock_log_correlation_id` | `correlation_id` | No |
| `IX_stock_log_location_id` | `location_id` | No |
| `IX_stock_log_stock_row_id` | `stock_row_id` | No |
| `ix_stock_log_tenant_id` | `tenant_id` | No |
| `ix_stock_log_transaction_id` | `transaction_id` | No |
| `ix_stock_log_user_id` | `user_id` | No |
| `ix_stock_log_performance1` | `stock_id`, `transaction_type`, `amount` | No |
| `ix_stock_log_performance2` | `product_id`, `best_before_date`, `purchased_date`, `transaction_type`, `stock_id`, `undone` | No |

## Relationships

- **stock_log** (`location_id`) → **locations** (`Id`)
- **stock_log** (`product_id`) → **products** (`Id`)
- **stock_log** (`stock_row_id`) → **stock** (`id`)
- **stock_log** (`user_id`) → **users** (`Id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

