# chores

**Schema:** `public`  
**Table:** `chores`  
**Entity:** `Chore`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `assignment_config` | `text` | Yes |  |  | |
| `assignment_type` | `character varying(50)` | Yes |  |  | |
| `consume_product_on_execution` | `smallint` | No |  |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `description` | `text` | Yes |  |  | |
| `name` | `character varying(255)` | No |  |  | |
| `next_execution_assigned_to_user_id` | `uuid` | Yes | FK→users |  | |
| `period_days` | `integer` | Yes |  |  | |
| `period_type` | `character varying(50)` | No |  |  | |
| `product_amount` | `numeric(18,4)` | Yes |  |  | |
| `product_id` | `uuid` | Yes | FK→products |  | |
| `rollover` | `smallint` | No |  |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `track_date_only` | `smallint` | No |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `IX_chores_next_execution_assigned_to_user_id` | `next_execution_assigned_to_user_id` | No |
| `IX_chores_product_id` | `product_id` | No |
| `ix_chores_tenant_id` | `tenant_id` | No |
| `ux_chores_tenant_name` | `tenant_id`, `name` | Yes |

## Relationships

- **chores** (`next_execution_assigned_to_user_id`) → **users** (`Id`)
- **chores** (`product_id`) → **products** (`Id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

