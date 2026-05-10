# Schema Comparison: Outstanding Issues

---

## 1. Missing Columns — `fsbs_schema` has columns EF does not

These columns exist in `fsbs_schema` but not in the EF model. A decision is needed on whether to drop them or add them to the EF model.

| Table | Column(s) only in `fsbs_schema` |
|---|---|
| `app_users` | `is_active` |
| `user_profiles` | `date_of_birth`, `licence_number`, `licence_expiry`, `photo_s3_key` |
| `org_memberships` | `joined_at`, `is_active` |
| `invitations` | `issued_by`, `issued_at`, `personal_note` |
| `modules` | `description` |
| `reconfiguration_templates` | `notes` |
| `instructor_availabilities` | *(none — aligned)* |

---

## 2. Missing Columns — EF has columns `fsbs_schema` does not

| Table | Column(s) only in EF |
|---|---|
| `account_statements` | `org_account_id` (nullable FK to `org_accounts`) |

---

## 3. `account_payments` — FK target mismatch

`fsbs_schema` has `account_id` referencing `org_accounts (account_id)` (the PK).
EF names the column `org_account_id` but its FK constraint still targets `org_accounts (account_id)` — the PK has not been renamed in EF.

`fsbs_schema` must keep `account_id` as the PK on `org_accounts` (matching EF), but the FK column in `account_payments` was renamed to `org_account_id` in a prior session. This is now inconsistent — `fsbs_schema` has `account_id` as the PK but `org_account_id` as the FK column in `account_payments`.

**Action:** Revert `account_payments.org_account_id` back to `account_id` in `fsbs_schema`, or rename the PK column in `org_accounts` to `org_account_id` to match.

---

## 4. CHECK Constraints — present in `fsbs_schema`, missing from EF

EF will need these added via `migrationBuilder.Sql(...)`:

| Table | Constraint | `fsbs_schema` | EF |
|---|---|---|---|
| `reconfiguration_templates` | `ck_reconfig_templates_duration` — `duration_mins > 0` | ✅ | ❌ |
| `pricing_policies` | `ck_pricing_policies_rate` — `hourly_rate_gbp >= 0` | ✅ | ❌ |
| `pricing_policies` | `ck_pricing_policies_dates` — `effective_to IS NULL OR effective_to > effective_from` | ✅ | ❌ |
| `schedule_templates` | `ck_schedule_templates_day`, `ck_schedule_templates_times`, `ck_schedule_templates_dates` | ✅ | ❌ |
| `account_statements` | `ck_account_statements_period` — `period_end >= period_start` | ✅ | ❌ |
| `instructors` | `ck_instructors_ratings` — `array_length >= 1` | ✅ | ❌ |
| `lessons` | `ck_lessons_min_duration` — `min_duration_mins > 0` | ✅ | ❌ |

---

## 5. ENUMs — `fsbs_schema` uses native PG enums, EF uses `text`

`fsbs_schema` defines rich native PG enums for all status/type columns. EF maps most of these as `text`. The EF model only defines one native enum: `fsbs.training_type`.

Columns affected (EF uses `text`, `fsbs_schema` uses a typed enum):

| Table | Column | `fsbs_schema` type | EF type |
|---|---|---|---|
| `app_users` | `app_role` | `app_role` (enum) | `text` |
| `bookings` | `status` | `booking_status` (enum) | `text` |
| `bookings` | `booker_role` | `app_role` (enum) | `text` |
| `booking_slots` | `slot_status` | `slot_status` (enum) | `text` |
| `enrolments` | `status` | `enrolment_status` (enum) | `text` |
| `invitations` | `status` | `invitation_status` (enum) | `text` |
| `invitations` | `invitee_role` | `invitee_role` (enum) | `text` |
| `account_payments` | `payment_method` | `payment_method` (enum) | `text` |
| `account_payments` | `status` | `payment_status` (enum) | `text` |
| `org_accounts` | `status` | `account_status` (enum) | `text` |
| `simulator_bays` | `status` | `bay_status` (enum) | `text` |
| `instructor_availabilities` | `avail_type` | `availability_type` (enum) | `text` |
| `discount_rules` | `discount_type` | `discount_type` (enum) | `text` |
| `booking_discounts` | `discount_type` | `discount_type` (enum) | `text` |
| `booking_approvals` | `decision` | `approval_decision` (enum) | `text` |
| `report_runs` | `status` | `report_run_status` (enum) | `text` |
| `invoices` | `status` | `invoice_status` (enum) | `text` |
| `org_memberships` | `org_role` | `org_role` (enum) | `text` |
| `organisations` | `customer_class` | `customer_class` (enum) | `text` |
| `pricing_policies` | `customer_class` | `customer_class` (enum) | `text` |

These must be added to the EF migration as raw SQL `CREATE TYPE … AS ENUM` statements, with the columns altered to use them.

---

## 6. Triggers — missing from EF

`fsbs_schema` defines two triggers maintaining `org_accounts.current_balance_gbp`:

```sql
CREATE TRIGGER trg_invoices_update_balance  AFTER INSERT OR UPDATE OR DELETE ON fsbs.invoices ...
CREATE TRIGGER trg_payments_update_balance  AFTER INSERT OR UPDATE OR DELETE ON fsbs.account_payments ...
```

EF has no triggers. These must be added via `migrationBuilder.Sql(...)`.

---

## 7. Row-Level Security — missing from EF

`fsbs_schema` enables RLS and defines a `tenant_isolation` policy on six tables. EF has no `ALTER TABLE … ENABLE ROW LEVEL SECURITY` or `CREATE POLICY` statements. Must be added via raw SQL in the migration.

---

## 8. `simulator_configurations.simulator_unit_id` — nullable vs NOT NULL

| Schema | `simulator_unit_id` nullability |
|---|---|
| `fsbs_schema` | `NULL` (optional FK, deferred constraint) |
| EF | `NOT NULL` |

Needs a decision: EF treats every configuration as belonging to a unit at creation time; `fsbs_schema` allows configurations to exist independently. Align before migration.

---

## 9. `org_accounts` — missing `credit_limit_gbp` CHECK in EF

`fsbs_schema` has `ck_org_accounts_credit_limit CHECK (credit_limit_gbp >= 0)`. EF omits this. Add via raw SQL in migration.

---

## 10. `invitations` — `claimed_by` FK missing in EF

`fsbs_schema` has explicit FKs for `issued_by`, `claimed_by`, and `revoked_by` referencing `app_users`. EF only stores `claimed_by` as a bare `uuid` with no FK constraint. Add FK constraints via raw SQL in migration or update the EF model.
