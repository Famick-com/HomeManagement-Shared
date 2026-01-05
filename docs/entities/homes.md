# homes

**Schema:** `public`  
**Table:** `homes`  
**Entity:** `Home`

## Primary Key

- `id`

## Columns

| Column | Type | Nullable | Key | Default | Description |
|--------|------|----------|-----|---------|-------------|
| `id` | `uuid` | No | PK |  | |
| `ac_filter_replacement_interval_days` | `integer` | Yes |  |  | |
| `ac_filter_sizes` | `character varying(255)` | Yes |  |  | |
| `address` | `character varying(500)` | No |  |  | |
| `appraisal_date` | `timestamp with time zone` | Yes |  |  | |
| `appraisal_value` | `numeric(18,2)` | Yes |  |  | |
| `bathrooms` | `numeric(3,1)` | Yes |  |  | |
| `bedrooms` | `integer` | Yes |  |  | |
| `created_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `escrow_details` | `text` | Yes |  |  | |
| `fridge_water_filter_type` | `character varying(100)` | Yes |  |  | |
| `hoa_contact_info` | `text` | Yes |  |  | |
| `hoa_name` | `character varying(255)` | Yes |  |  | |
| `hoa_rules_link` | `character varying(1000)` | Yes |  |  | |
| `hvac_service_schedule` | `text` | Yes |  |  | |
| `insurance_agent_email` | `character varying(255)` | Yes |  |  | |
| `insurance_agent_name` | `character varying(255)` | Yes |  |  | |
| `insurance_agent_phone` | `character varying(50)` | Yes |  |  | |
| `insurance_policy_number` | `character varying(100)` | Yes |  |  | |
| `insurance_type` | `integer` | Yes |  |  | |
| `is_setup_complete` | `boolean` | No |  |  | |
| `mortgage_info` | `text` | Yes |  |  | |
| `pest_control_schedule` | `text` | Yes |  |  | |
| `property_tax_account_number` | `character varying(100)` | Yes |  |  | |
| `smoke_co_detector_battery_type` | `character varying(50)` | Yes |  |  | |
| `square_footage` | `integer` | Yes |  |  | |
| `tenant_id` | `uuid` | No |  |  | |
| `under_sink_filter_type` | `character varying(100)` | Yes |  |  | |
| `unit` | `character varying(50)` | Yes |  |  | |
| `updated_at` | `timestamp with time zone` | No |  | CURRENT_TIMESTAMP | |
| `whole_house_filter_type` | `character varying(100)` | Yes |  |  | |
| `year_built` | `integer` | Yes |  |  | |

## Indexes

| Name | Columns | Unique |
|------|---------|--------|
| `ux_homes_tenant_id` | `tenant_id` | Yes |

## Relationships


## Notes

<!-- NOTES_START -->
<!-- Add your notes here. This section is preserved when regenerating. -->
<!-- NOTES_END -->

