# chores_log

**Schema:** `public`  
**Table:** `chores_log`  
**Entity:** `ChoreLog`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `chore_id` | `uuid` | No | FK→chores |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `done_by_user_id` | `uuid` | Yes | FK→users |  | |
| `scheduled_execution_time` | `timestamp with time zone` | Yes |  |  | |
| `skipped` | `smallint` | No |  |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `tracked_time` | `timestamp with time zone` | Yes |  |  | |
| `undone` | `smallint` | No |  |  | |
| `undone_timestamp` | `timestamp with time zone` | Yes |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `ix_chores_log_chore_id` | `chore_id` | No |
| `ix_chores_log_done_by_user_id` | `done_by_user_id` | No |
| `ix_chores_log_tenant_id` | `tenant_id` | No |

## Relationships

- **chores_log** (`chore_id`) → **chores** (`id`)
- **chores_log** (`done_by_user_id`) → **users** (`Id`)

## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

