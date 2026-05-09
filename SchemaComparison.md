# Schema Comparison: `ef_generated_schema.sql` vs `fsbs_schema.sql`

---

## 1. Tables Present Only in EF — `aircraft_types`

`fsbs_schema.sql` has no `aircraft_types` table. Instead, `simulator_configurations` stores `aircraft_type` as a plain `varchar` column. The EF model promotes this to a proper lookup table with its own UUID PK, `icao_code`, `name`, `is_active`, and soft-delete/audit columns, and `simulator_configurations` then holds a FK `aircraft_type_id` referencing it.

---

## 2. Table Naming Differences

| `fsbs_schema.sql`        | `ef_generated_schema.sql`      |
|--------------------------|--------------------------------|
| `users`                  | `app_users`                    |
| `instructor_availability`| `instructor_availabilities`    |

---

## 3. Column Name Differences (same concept, different name)

| Table                    | `fsbs_schema.sql`       | `ef_generated_schema.sql`  |
|--------------------------|-------------------------|----------------------------|
| `account_payments`       | `account_id`            | `org_account_id`           |
| `account_statements`     | `statement_s3_key`      | `statement_s3key`          |
| `app_users` / `users`    | `role`                  | `app_role`                 |
| `booking_discounts`      | `rule_id`               | `discount_rule_id`         |
| `maintenance_windows`    | `window_id`             | `maintenance_window_id`    |
| `org_accounts`           | `account_status`        | `status`                   |
| `payment_allocations`    | `allocated_amount_gbp`  | `amount_gbp`               |
| `pricing_policies`       | `config_id`             | `configuration_id`         |
| `progress_records`       | `progress_id`           | `progress_record_id`       |
| `qualifications`         | `document_s3_key`       | `document_s3key`           |
| `report_runs`            | `result_s3_key`         | `result_s3key`             |
| `simulator_bays`         | `unit_id`               | `simulator_unit_id`        |
| `simulator_units`        | `active_config_id`      | `active_configuration_id`  |

---

## 4. Columns Present in EF but Missing from `fsbs_schema`

| Table                      | Columns only in EF                                          |
|----------------------------|-------------------------------------------------------------|
| `account_payments`         | `verified_at`, `void_reason`                                |
| `bookings`                 | `booker_role`                                               |
| `courses`                  | `description`                                               |
| `instructor_availabilities`| `notes`                                                     |
| `invitations`              | `claimed_by` (renamed from `claimed_by_user_id`)            |
| `organisations`            | `customer_class`                                            |
| `simulator_configurations` | `aircraft_type_id` (FK to new table), `simulator_unit_id`  |

---

## 5. Columns Present in `fsbs_schema` but Missing from EF

| Table                    | Columns only in `fsbs_schema`                                                              |
|--------------------------|--------------------------------------------------------------------------------------------|
| `users` / `app_users`    | `is_active`                                                                                |
| `bookings`               | `bay_id`, `instructor_id` (moved to `booking_slots` in EF)                                 |
| `invitations`            | `issued_at`, `issued_by`, `personal_note`, `claimed_by_user_id`                            |
| `org_memberships`        | `is_active`, `joined_at`                                                                   |
| `user_profiles`          | `date_of_birth`, `licence_number`, `licence_expiry`, `phone` (vs `phone_number` in EF), `photo_s3_key` |
| `modules`                | `description`                                                                              |
| `reconfiguration_templates` | `notes`                                                                                 |

---

## 6. ENUMs

`fsbs_schema.sql` defines **no** PostgreSQL `CREATE TYPE … AS ENUM` statements — it relies on `varchar` columns with `CHECK` constraints or EF-side validation.

`ef_generated_schema.sql` creates one native PG enum: `fsbs.training_type AS ENUM ('flight_deck', 'cabin_crew')`, used in `bookings` and `booking_slots`. The guidelines call for native PG enums for all status/type columns — so `fsbs_schema` is under-specified here.

---

## 7. CHECK Constraints — Missing from `fsbs_schema`

| Constraint | EF | `fsbs_schema` |
|---|---|---|
| `ck_booking_slots_min_duration` — `duration_mins >= 240` | ✅ | ❌ **Missing** |
| `ck_bookings_fd_capacity` — `training_type != 'flight_deck' OR student_count <= 4` | ✅ | ❌ **Missing** |
| `ck_bookings_cc_capacity` — `training_type != 'cabin_crew' OR student_count <= 10` | ✅ | ❌ **Missing** |
| `ck_instructors_hours` — upper bound `<= 168` | ✅ | ❌ Missing upper bound |
| `ck_modules_sequence` / `ck_lessons_sequence` — `>= 1` | ✅ | ⚠️ `> 0` (equivalent, different style) |
| `ck_reconfig_templates_duration` | ❌ Missing | ✅ |
| `ck_pricing_policies_rate` / `ck_pricing_policies_dates` | ❌ Missing | ✅ |
| `ck_schedule_templates_*` | ❌ Missing | ✅ |
| `ck_account_statements_period` | ❌ Missing | ✅ |

The three **most critical** gaps in `fsbs_schema` are the 240-minute minimum booking duration and the FlightDeck/CabinCrew capacity caps — explicitly required as DB-level enforcement in the guidelines.

---

## 8. Triggers — Missing from EF

`fsbs_schema.sql` defines two triggers that maintain `org_accounts.current_balance_gbp`:

```sql
CREATE TRIGGER trg_invoices_update_balance  AFTER INSERT OR UPDATE OR DELETE ON fsbs.invoices ...
CREATE TRIGGER trg_payments_update_balance  AFTER INSERT OR UPDATE OR DELETE ON fsbs.account_payments ...
```

**The EF-generated schema has no triggers at all.** The balance column will not be maintained automatically if the EF schema is applied as-is. These must be added as raw SQL via `migrationBuilder.Sql(...)` in the EF migration.

---

## 9. Row-Level Security — Missing from EF

`fsbs_schema.sql` enables RLS and defines a `tenant_isolation` policy on the six tenant-scoped tables. The EF-generated schema has **no** `ALTER TABLE … ENABLE ROW LEVEL SECURITY` or `CREATE POLICY` statements. These must also be applied via raw SQL in migrations.

---

## 10. Unique Indexes — EF has significantly more

`fsbs_schema.sql` defines only 3 unique/partial indexes. The EF schema defines 17:

| Index | EF | `fsbs_schema` |
|---|---|---|
| `uq_invitations_token_hash` | ✅ | ❌ **Missing** (security requirement) |
| `uq_bookings_idempotency_key` | ✅ | ❌ **Missing** |
| `uq_enrolments_user_course` | ✅ | ❌ Missing |
| `uq_reconfig_templates_pair` | ✅ | ❌ Missing |
| `uq_reconfig_slots_bay_time` | ✅ | ❌ Missing |
| `uq_instructors_user` / `uq_instructors_employee_number` | ✅ | ❌ Missing |
| `uq_app_users_cognito_sub` / `uq_app_users_email` | ✅ | ❌ Missing |
| `uq_booking_slots_bay_time` (partial, `WHERE slot_status != 'Cancelled'`) | ✅ | ✅ |
| `uq_invitations_pending_email_org` (partial) | ✅ | ✅ |

The missing `uq_invitations_token_hash` is a security gap — the guidelines explicitly require it.

---

## Summary: What Needs to Be Reconciled

| Category | Action needed |
|---|---|
| `aircraft_types` table | Add to `fsbs_schema.sql` or accept EF as authoritative |
| Table/column renames | Align naming (`users`→`app_users`, `s3_key` vs `s3key`, etc.) |
| Missing columns (`user_profiles`, `invitations`, `org_memberships`, etc.) | Decide which schema is correct; `fsbs_schema` appears incomplete in several places |
| 3 critical CHECK constraints (min duration, FD/CC capacity) | **Add to `fsbs_schema.sql`** — currently a compliance gap |
| Balance triggers | Add as raw SQL in EF migration (`migrationBuilder.Sql(...)`) |
| RLS policies | Add as raw SQL in EF migration |
| 14 missing unique indexes in `fsbs_schema` | Add them — `uq_invitations_token_hash` is a security requirement |
