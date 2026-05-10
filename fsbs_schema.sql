-- =============================================================================
-- Flight Simulator Booking System (FSBS)
-- PostgreSQL 16 DDL — Schema: fsbs
-- =============================================================================
-- Conventions:
--   • All tables in the fsbs schema
--   • snake_case column names (mirrors EFCore.NamingConventions)
--   • UUID primary keys with uuid_generate_v4() default
--   • All timestamps: timestamptz (DateTimeOffset in C#)
--   • Soft deletes: is_deleted boolean + EF Core global query filter
--   • Audit columns on every table: created_at, updated_at, created_by, updated_by
--   • Optimistic concurrency: xmin system column (no explicit column needed)
-- =============================================================================

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create schema
CREATE SCHEMA IF NOT EXISTS fsbs;

SET search_path = fsbs, public;

-- =============================================================================
-- ENUMS
-- =============================================================================

CREATE TYPE app_role AS ENUM (
    'SystemAdmin',
    'ScheduleAdmin',
    'CourseDirector',
    'Instructor',
    'Management',
    'SalesStaff',
    'InternalStudent',
    'PrivateCustomer',
    'CorporateManager',
    'CorporateStudent'
);

CREATE TYPE training_type AS ENUM (
    'FlightDeck',
    'CabinCrew'
);

CREATE TYPE configuration_mode AS ENUM (
    'CockpitOnly',
    'CockpitAndCabin'
);

CREATE TYPE booking_status AS ENUM (
    'Provisional',
    'PendingApproval',
    'Confirmed',
    'InProgress',
    'Completed',
    'Invoiced',
    'CancelledByCustomer',
    'CancelledByAdmin',
    'Rejected',
    'Expired',
    'OnHold'
);

CREATE TYPE slot_status AS ENUM (
    'Scheduled',
    'InProgress',
    'Completed',
    'Cancelled'
);

CREATE TYPE enrolment_status AS ENUM (
    'Active',
    'Completed',
    'Withdrawn',
    'Suspended'
);

CREATE TYPE invitation_status AS ENUM (
    'pending',
    'claimed',
    'expired',
    'revoked'
);

CREATE TYPE invitee_role AS ENUM (
    'corporate_manager',
    'corporate_student'
);

CREATE TABLE payment_methods (
    code        varchar(50)     NOT NULL,
    label       varchar(100)    NOT NULL,
    is_active   boolean         NOT NULL DEFAULT true,
    CONSTRAINT pk_payment_methods PRIMARY KEY (code)
);
INSERT INTO payment_methods (code, label) VALUES
    ('BankTransfer', 'Bank Transfer'),
    ('Cheque',       'Cheque'),
    ('Cash',         'Cash'),
    ('CreditNote',   'Credit Note'),
    ('Adjustment',   'Adjustment');

CREATE TYPE payment_status AS ENUM (
    'Pending',
    'Verified',
    'Voided'
);

CREATE TABLE account_statuses (
    code            varchar(50)     NOT NULL,
    label           varchar(100)    NOT NULL,
    allows_booking  boolean         NOT NULL DEFAULT true,
    CONSTRAINT pk_account_statuses PRIMARY KEY (code)
);
INSERT INTO account_statuses (code, label, allows_booking) VALUES
    ('Active',    'Active',    true),
    ('Suspended', 'Suspended', false),
    ('Closed',    'Closed',    false);

CREATE TYPE bay_status AS ENUM (
    'operational',
    'maintenance',
    'decommissioned'
);

CREATE TYPE availability_type AS ENUM (
    'available',
    'leave',
    'other'
);

CREATE TABLE discount_types (
    code        varchar(50)     NOT NULL,
    label       varchar(100)    NOT NULL,
    is_active   boolean         NOT NULL DEFAULT true,
    CONSTRAINT pk_discount_types PRIMARY KEY (code)
);
INSERT INTO discount_types (code, label) VALUES
    ('VolumeAdvanceBlock',  'Volume Advance Block'),
    ('VolumeOrgSession',    'Volume Org Session'),
    ('AdvanceBooking',      'Advance Booking'),
    ('CorporateNegotiated', 'Corporate Negotiated'),
    ('StaffRate',           'Staff Rate'),
    ('Promotional',         'Promotional');

CREATE TABLE customer_classes (
    code        varchar(50)     NOT NULL,
    label       varchar(100)    NOT NULL,
    is_active   boolean         NOT NULL DEFAULT true,
    CONSTRAINT pk_customer_classes PRIMARY KEY (code)
);
INSERT INTO customer_classes (code, label) VALUES
    ('Standard',  'Standard'),
    ('Staff',     'Staff'),
    ('Corporate', 'Corporate');

CREATE TYPE approval_decision AS ENUM (
    'Pending',
    'Approved',
    'Rejected'
);

CREATE TYPE report_run_status AS ENUM (
    'Queued',
    'Running',
    'Completed',
    'Failed'
);

CREATE TYPE invoice_status AS ENUM (
    'Draft',
    'Issued',
    'Paid',
    'Overdue',
    'Voided'
);

CREATE TYPE org_role AS ENUM (
    'manager',
    'student'
);

-- =============================================================================
-- IDENTITY & USERS
-- =============================================================================

CREATE TABLE app_users (
    user_id             uuid            NOT NULL DEFAULT uuid_generate_v4(),
    cognito_sub         varchar(128)    NOT NULL,
    email               character varying(256) NOT NULL,
    app_role            app_role        NOT NULL,
    tenant_id           uuid            NOT NULL,
    is_active           boolean         NOT NULL DEFAULT true,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_app_users PRIMARY KEY (user_id),
    CONSTRAINT uq_users_cognito_sub UNIQUE (cognito_sub),
    CONSTRAINT uq_users_email UNIQUE (email)
);

CREATE INDEX ix_app_users_tenant_id ON app_users (tenant_id) WHERE is_deleted = false;
CREATE INDEX ix_app_users_app_role ON app_users (app_role) WHERE is_deleted = false;

-- ----------------------------------------------------------------------------

CREATE TABLE user_profiles (
    user_id             uuid            NOT NULL,
    first_name          varchar(100)    NOT NULL,
    last_name           varchar(100)    NOT NULL,
    phone_number        varchar(30)     NULL,
    date_of_birth       date            NULL,
    licence_number      varchar(50)     NULL,
    licence_expiry      date            NULL,
    photo_s3_key        varchar(500)    NULL,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_user_profiles PRIMARY KEY (user_id),
    CONSTRAINT fk_user_profiles_app_users FOREIGN KEY (user_id)
        REFERENCES app_users (user_id) ON DELETE CASCADE
);

-- ----------------------------------------------------------------------------

CREATE TABLE qualifications (
    qualification_id    uuid            NOT NULL DEFAULT uuid_generate_v4(),
    user_id             uuid            NOT NULL,
    type                varchar(100)    NOT NULL,
    issued_date         date            NOT NULL,
    expiry_date         date            NULL,
    document_s3key      varchar(500)    NULL,
    verified_by         uuid            NULL,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_qualifications PRIMARY KEY (qualification_id),
    CONSTRAINT fk_qualifications_app_users FOREIGN KEY (user_id)
        REFERENCES app_users (user_id) ON DELETE CASCADE,
    CONSTRAINT fk_qualifications_verified_by FOREIGN KEY (verified_by)
        REFERENCES app_users (user_id) ON DELETE SET NULL
);

CREATE INDEX ix_qualifications_user_id ON qualifications (user_id) WHERE is_deleted = false;
CREATE INDEX ix_qualifications_expiry ON qualifications (expiry_date) WHERE is_deleted = false AND expiry_date IS NOT NULL;

-- =============================================================================
-- ORGANISATIONS & ACCOUNTS
-- =============================================================================

CREATE TABLE organisations (
    org_id              uuid            NOT NULL DEFAULT uuid_generate_v4(),
    name                varchar(200)    NOT NULL,
    tenant_id           uuid            NOT NULL,
    contract_type       varchar(50)     NULL,
    credit_limit_gbp    numeric(12,2)   NOT NULL DEFAULT 0,
    is_active           boolean         NOT NULL DEFAULT true,
    billing_email       varchar(255)    NOT NULL,
    customer_class      varchar(50)     NOT NULL DEFAULT 'Standard',
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_organisations PRIMARY KEY (org_id),
    CONSTRAINT fk_organisations_customer_class FOREIGN KEY (customer_class) REFERENCES customer_classes (code) ON DELETE RESTRICT,
    CONSTRAINT ck_organisations_credit_limit CHECK (credit_limit_gbp >= 0)
);

CREATE INDEX ix_organisations_tenant_id ON organisations (tenant_id) WHERE is_deleted = false;

-- ----------------------------------------------------------------------------

CREATE TABLE org_memberships (
    membership_id       uuid            NOT NULL DEFAULT uuid_generate_v4(),
    org_id              uuid            NOT NULL,
    user_id             uuid            NOT NULL,
    org_role            org_role        NOT NULL,
    joined_at           timestamptz     NOT NULL DEFAULT now(),
    is_active           boolean         NOT NULL DEFAULT true,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_org_memberships PRIMARY KEY (membership_id),
    CONSTRAINT fk_org_memberships_org FOREIGN KEY (org_id)
        REFERENCES organisations (org_id) ON DELETE CASCADE,
    CONSTRAINT fk_org_memberships_user FOREIGN KEY (user_id)
        REFERENCES app_users (user_id) ON DELETE CASCADE,
    CONSTRAINT uq_org_memberships_org_user UNIQUE (org_id, user_id)
);

CREATE INDEX ix_org_memberships_user_id ON org_memberships (user_id) WHERE is_deleted = false;

-- ----------------------------------------------------------------------------

CREATE TABLE org_accounts (
    account_id              uuid            NOT NULL DEFAULT uuid_generate_v4(),
    org_id                  uuid            NOT NULL,
    credit_limit_gbp        numeric(12,2)   NOT NULL DEFAULT 0,
    current_balance_gbp     numeric(12,2)   NOT NULL DEFAULT 0,
    payment_terms_days      integer         NOT NULL DEFAULT 30,
    status                  varchar(50)     NOT NULL DEFAULT 'Active',
    created_at              timestamptz     NOT NULL DEFAULT now(),
    updated_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              uuid            NULL,
    updated_by              uuid            NULL,
    CONSTRAINT pk_org_accounts PRIMARY KEY (account_id),
    CONSTRAINT fk_org_accounts_status FOREIGN KEY (status) REFERENCES account_statuses (code) ON DELETE RESTRICT,
    CONSTRAINT fk_org_accounts_org FOREIGN KEY (org_id)
        REFERENCES organisations (org_id) ON DELETE CASCADE,
    CONSTRAINT uq_org_accounts_org_id UNIQUE (org_id),
    CONSTRAINT ck_org_accounts_credit_limit CHECK (credit_limit_gbp >= 0),
    CONSTRAINT ck_org_accounts_payment_terms CHECK (payment_terms_days > 0)
);

-- ----------------------------------------------------------------------------

CREATE TABLE account_payments (
    payment_id          uuid            NOT NULL DEFAULT uuid_generate_v4(),
    org_id              uuid            NOT NULL,
    org_account_id      uuid            NOT NULL,
    amount_gbp          numeric(12,2)   NOT NULL,
    payment_method      varchar(50)     NOT NULL,
    payment_date        date            NOT NULL,
    reference           character varying(200) NULL,
    notes               text            NULL,
    recorded_by         uuid            NOT NULL,
    verified_by         uuid            NULL,
    status              payment_status  NOT NULL DEFAULT 'Pending',
    verified_at         timestamptz     NULL,
    void_reason         character varying(500) NULL,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_account_payments PRIMARY KEY (payment_id),
    CONSTRAINT fk_account_payments_payment_method FOREIGN KEY (payment_method) REFERENCES payment_methods (code) ON DELETE RESTRICT,
    CONSTRAINT fk_account_payments_org FOREIGN KEY (org_id)
        REFERENCES organisations (org_id),
    CONSTRAINT fk_account_payments_account FOREIGN KEY (org_account_id)
        REFERENCES org_accounts (account_id),
    CONSTRAINT fk_account_payments_recorded_by FOREIGN KEY (recorded_by)
        REFERENCES app_users (user_id),
    CONSTRAINT fk_account_payments_verified_by FOREIGN KEY (verified_by)
        REFERENCES app_users (user_id) ON DELETE SET NULL,
    CONSTRAINT ck_account_payments_amount CHECK (amount_gbp > 0)
);

CREATE INDEX ix_account_payments_org_id ON account_payments (org_id) WHERE is_deleted = false;
CREATE INDEX ix_account_payments_status ON account_payments (status) WHERE is_deleted = false;

-- ----------------------------------------------------------------------------

CREATE TABLE payment_allocations (
    allocation_id           uuid            NOT NULL DEFAULT uuid_generate_v4(),
    payment_id              uuid            NOT NULL,
    invoice_id              uuid            NOT NULL,  -- FK added after invoices table
    amount_gbp              numeric(12,2)   NOT NULL,
    created_at              timestamptz     NOT NULL DEFAULT now(),
    updated_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              uuid            NULL,
    updated_by              uuid            NULL,
    CONSTRAINT pk_payment_allocations PRIMARY KEY (allocation_id),
    CONSTRAINT fk_payment_allocations_payment FOREIGN KEY (payment_id)
        REFERENCES account_payments (payment_id) ON DELETE CASCADE,
    CONSTRAINT ck_payment_allocations_amount CHECK (amount_gbp > 0)
);

CREATE INDEX ix_payment_allocations_payment_id ON payment_allocations (payment_id);

-- =============================================================================
-- INVITATIONS
-- =============================================================================

CREATE TABLE invitations (
    invitation_id           uuid                NOT NULL DEFAULT uuid_generate_v4(),
    token_hash              char(64)            NOT NULL,  -- SHA-256 hex; raw token NEVER stored
    invitee_email           varchar(255)        NOT NULL,
    invitee_role            invitee_role        NOT NULL,
    org_id                  uuid                NOT NULL,
    issued_by               uuid                NOT NULL,
    issued_at               timestamptz         NOT NULL DEFAULT now(),
    expires_at              timestamptz         NOT NULL DEFAULT (now() + interval '7 days'),
    status                  invitation_status   NOT NULL DEFAULT 'Pending',
    claimed_by              uuid                NULL,
    claimed_at              timestamptz         NULL,
    revoked_by              uuid                NULL,
    revoked_at              timestamptz         NULL,
    personal_note           text                NULL,
    created_at              timestamptz         NOT NULL DEFAULT now(),
    updated_at              timestamptz         NOT NULL DEFAULT now(),
    created_by              uuid                NULL,
    updated_by              uuid                NULL,
    CONSTRAINT pk_invitations PRIMARY KEY (invitation_id),
    CONSTRAINT fk_invitations_org FOREIGN KEY (org_id)
        REFERENCES organisations (org_id),
    CONSTRAINT fk_invitations_issued_by FOREIGN KEY (issued_by)
        REFERENCES app_users (user_id),
    CONSTRAINT fk_invitations_claimed_by FOREIGN KEY (claimed_by)
        REFERENCES app_users (user_id) ON DELETE SET NULL,
    CONSTRAINT fk_invitations_revoked_by FOREIGN KEY (revoked_by)
        REFERENCES app_users (user_id) ON DELETE SET NULL,
    CONSTRAINT uq_invitations_token_hash UNIQUE (token_hash),
    CONSTRAINT ck_invitations_claimed CHECK (
        (status = 'claimed' AND claimed_by IS NOT NULL AND claimed_at IS NOT NULL)
        OR (status != 'claimed')
    ),
    CONSTRAINT ck_invitations_revoked CHECK (
        (status = 'revoked' AND revoked_by IS NOT NULL AND revoked_at IS NOT NULL)
        OR (status != 'revoked')
    )
);

-- Active invitations: prevent duplicate pending invites to same email+org
CREATE UNIQUE INDEX uq_invitations_pending_email_org
    ON invitations (invitee_email, org_id)
    WHERE status = 'pending';

CREATE INDEX ix_invitations_org_id ON invitations (org_id);
CREATE INDEX ix_invitations_status ON invitations (status);
CREATE INDEX ix_invitations_expires_at ON invitations (expires_at) WHERE status = 'Pending';

-- =============================================================================
-- SIMULATORS & CONFIGURATIONS
-- =============================================================================

CREATE TABLE aircraft_types (
    aircraft_type_id            uuid                NOT NULL DEFAULT uuid_generate_v4(),
    icao_code                   varchar(20)         NOT NULL,
    name                        varchar(100)        NOT NULL,
    is_active                   boolean             NOT NULL DEFAULT true,
    is_deleted                  boolean             NOT NULL DEFAULT false,
    created_at                  timestamptz         NOT NULL DEFAULT now(),
    updated_at                  timestamptz         NOT NULL DEFAULT now(),
    created_by                  uuid                NULL,
    updated_by                  uuid                NULL,
    CONSTRAINT pk_aircraft_types PRIMARY KEY (aircraft_type_id)
);

CREATE UNIQUE INDEX uq_aircraft_types_icao_code ON aircraft_types (icao_code) WHERE is_deleted = false;

CREATE TABLE simulator_configurations (
    config_id                   uuid                NOT NULL DEFAULT uuid_generate_v4(),
    name                        varchar(200)        NOT NULL,
    aircraft_type_id            uuid                NOT NULL,
    config_mode                 configuration_mode  NOT NULL,
    supported_training_types    training_type[]     NOT NULL,
    max_capacity_flight_deck    integer             NOT NULL DEFAULT 4,
    max_capacity_cabin_crew     integer             NOT NULL DEFAULT 10,
    simulator_unit_id           uuid                NULL,
    is_active                   boolean             NOT NULL DEFAULT true,
    is_deleted                  boolean             NOT NULL DEFAULT false,
    created_at                  timestamptz         NOT NULL DEFAULT now(),
    updated_at                  timestamptz         NOT NULL DEFAULT now(),
    created_by                  uuid                NULL,
    updated_by                  uuid                NULL,
    CONSTRAINT pk_simulator_configurations PRIMARY KEY (config_id),
    CONSTRAINT ck_simulator_config_fd_capacity CHECK (max_capacity_flight_deck > 0 AND max_capacity_flight_deck <= 4),
    CONSTRAINT ck_simulator_config_cc_capacity CHECK (max_capacity_cabin_crew > 0 AND max_capacity_cabin_crew <= 10),
    CONSTRAINT ck_simulator_config_training_types CHECK (array_length(supported_training_types, 1) >= 1),
    CONSTRAINT fk_simulator_configurations_aircraft_types FOREIGN KEY (aircraft_type_id) REFERENCES aircraft_types (aircraft_type_id) ON DELETE RESTRICT
);

-- ----------------------------------------------------------------------------

CREATE TABLE reconfiguration_templates (
    reconfig_template_id uuid           NOT NULL DEFAULT uuid_generate_v4(),
    from_config_id      uuid            NOT NULL,
    to_config_id        uuid            NOT NULL,
    duration_mins       integer         NOT NULL,
    notes               text            NULL,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_reconfiguration_templates PRIMARY KEY (reconfig_template_id),
    CONSTRAINT fk_reconfig_templates_from FOREIGN KEY (from_config_id)
        REFERENCES simulator_configurations (config_id),
    CONSTRAINT fk_reconfig_templates_to FOREIGN KEY (to_config_id)
        REFERENCES simulator_configurations (config_id),
    CONSTRAINT uq_reconfig_templates_pair UNIQUE (from_config_id, to_config_id),
    CONSTRAINT ck_reconfig_templates_duration CHECK (duration_mins > 0),
    CONSTRAINT ck_reconfig_templates_different CHECK (from_config_id != to_config_id)
);

-- ----------------------------------------------------------------------------

CREATE TABLE simulator_units (
    unit_id                 uuid            NOT NULL DEFAULT uuid_generate_v4(),
    name                    varchar(200)    NOT NULL,
    fstd_level              varchar(20)     NOT NULL,
    manufacturer            varchar(100)    NULL,
    location                varchar(200)    NULL,
    active_configuration_id uuid            NULL,
    default_reconfig_mins   integer         NOT NULL DEFAULT 60,
    is_active               boolean         NOT NULL DEFAULT true,
    is_deleted              boolean         NOT NULL DEFAULT false,
    created_at              timestamptz     NOT NULL DEFAULT now(),
    updated_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              uuid            NULL,
    updated_by              uuid            NULL,
    CONSTRAINT pk_simulator_units PRIMARY KEY (unit_id),
    CONSTRAINT fk_simulator_units_active_config FOREIGN KEY (active_configuration_id)
        REFERENCES simulator_configurations (config_id) ON DELETE SET NULL,
    CONSTRAINT ck_simulator_units_reconfig_mins CHECK (default_reconfig_mins > 0)
);

-- Deferred FK: simulator_configurations.simulator_unit_id → simulator_units
ALTER TABLE simulator_configurations
    ADD CONSTRAINT fk_simulator_configurations_unit FOREIGN KEY (simulator_unit_id)
        REFERENCES simulator_units (unit_id) ON DELETE SET NULL;

-- ----------------------------------------------------------------------------

CREATE TABLE simulator_bays (
    bay_id              uuid            NOT NULL DEFAULT uuid_generate_v4(),
    simulator_unit_id   uuid            NOT NULL,
    bay_code            varchar(20)     NOT NULL,
    status              bay_status      NOT NULL DEFAULT 'Operational',
    description         text            NULL,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_simulator_bays PRIMARY KEY (bay_id),
    CONSTRAINT fk_simulator_bays_unit FOREIGN KEY (simulator_unit_id)
        REFERENCES simulator_units (unit_id) ON DELETE CASCADE,
    CONSTRAINT uq_simulator_bays_code UNIQUE (simulator_unit_id, bay_code)
);

-- ----------------------------------------------------------------------------

CREATE TABLE maintenance_windows (
    maintenance_window_id uuid            NOT NULL DEFAULT uuid_generate_v4(),
    bay_id              uuid            NOT NULL,
    start_at            timestamptz     NOT NULL,
    end_at              timestamptz     NOT NULL,
    reason              text            NOT NULL,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_maintenance_windows PRIMARY KEY (maintenance_window_id),
    CONSTRAINT fk_maintenance_windows_bay FOREIGN KEY (bay_id)
        REFERENCES simulator_bays (bay_id) ON DELETE CASCADE,
    CONSTRAINT ck_maintenance_windows_range CHECK (end_at > start_at)
);

CREATE INDEX ix_maintenance_windows_bay_range ON maintenance_windows (bay_id, start_at, end_at)
    WHERE is_deleted = false;

-- =============================================================================
-- SCHEDULING
-- =============================================================================

CREATE TABLE schedule_templates (
    schedule_template_id uuid           NOT NULL DEFAULT uuid_generate_v4(),
    bay_id              uuid            NOT NULL,
    config_id           uuid            NOT NULL,
    day_of_week         integer         NOT NULL,  -- 0=Sunday ... 6=Saturday
    open_time           time            NOT NULL,
    close_time          time            NOT NULL,
    valid_from          date            NOT NULL,
    valid_to            date            NULL,
    is_active           boolean         NOT NULL DEFAULT true,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_schedule_templates PRIMARY KEY (schedule_template_id),
    CONSTRAINT fk_schedule_templates_bay FOREIGN KEY (bay_id)
        REFERENCES simulator_bays (bay_id),
    CONSTRAINT fk_schedule_templates_config FOREIGN KEY (config_id)
        REFERENCES simulator_configurations (config_id),
    CONSTRAINT ck_schedule_templates_day CHECK (day_of_week BETWEEN 0 AND 6),
    CONSTRAINT ck_schedule_templates_times CHECK (close_time > open_time),
    CONSTRAINT ck_schedule_templates_dates CHECK (valid_to IS NULL OR valid_to > valid_from)
);

CREATE INDEX ix_schedule_templates_bay ON schedule_templates (bay_id) WHERE is_deleted = false AND is_active = true;

-- =============================================================================
-- PRICING & DISCOUNTS
-- =============================================================================

CREATE TABLE pricing_policies (
    policy_id               uuid            NOT NULL DEFAULT uuid_generate_v4(),
    configuration_id       uuid            NOT NULL,
    training_type           training_type   NOT NULL,
    customer_class          varchar(50)     NOT NULL,
    hourly_rate_gbp         numeric(10,2)   NOT NULL,
    effective_from          date            NOT NULL,
    effective_to            date            NULL,
    is_deleted              boolean         NOT NULL DEFAULT false,
    created_at              timestamptz     NOT NULL DEFAULT now(),
    updated_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              uuid            NULL,
    updated_by              uuid            NULL,
    CONSTRAINT pk_pricing_policies PRIMARY KEY (policy_id),
    CONSTRAINT fk_pricing_policies_customer_class FOREIGN KEY (customer_class) REFERENCES customer_classes (code) ON DELETE RESTRICT,
    CONSTRAINT fk_pricing_policies_config FOREIGN KEY (configuration_id)
        REFERENCES simulator_configurations (config_id),
    CONSTRAINT ck_pricing_policies_rate CHECK (hourly_rate_gbp >= 0),
    CONSTRAINT ck_pricing_policies_dates CHECK (effective_to IS NULL OR effective_to > effective_from)
);

CREATE INDEX ix_pricing_policies_config ON pricing_policies (configuration_id, training_type, customer_class)
    WHERE is_deleted = false;

-- ----------------------------------------------------------------------------

CREATE TABLE discount_rules (
    discount_rule_id    uuid            NOT NULL DEFAULT uuid_generate_v4(),
    pricing_policy_id   uuid            NOT NULL,
    discount_type       varchar(50)     NOT NULL,
    priority            integer         NOT NULL DEFAULT 100,
    discount_pct        numeric(5,2)    NOT NULL,
    is_combinable       boolean         NOT NULL DEFAULT false,
    threshold_json      jsonb           NULL,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_discount_rules PRIMARY KEY (discount_rule_id),
    CONSTRAINT fk_discount_rules_discount_type FOREIGN KEY (discount_type) REFERENCES discount_types (code) ON DELETE RESTRICT,
    CONSTRAINT fk_discount_rules_policy FOREIGN KEY (pricing_policy_id)
        REFERENCES pricing_policies (policy_id) ON DELETE CASCADE,
    CONSTRAINT ck_discount_rules_pct CHECK (discount_pct >= 0 AND discount_pct <= 100)
);

-- =============================================================================
-- COURSES & LEARNING
-- =============================================================================

CREATE TABLE courses (
    course_id               uuid            NOT NULL DEFAULT uuid_generate_v4(),
    title                   varchar(300)    NOT NULL,
    description             text            NULL,
    training_type           training_type   NOT NULL,
    regulatory_framework    varchar(100)    NULL,
    total_hours             numeric(6,1)    NOT NULL,
    is_active               boolean         NOT NULL DEFAULT true,
    tenant_id               uuid            NOT NULL,
    is_deleted              boolean         NOT NULL DEFAULT false,
    created_at              timestamptz     NOT NULL DEFAULT now(),
    updated_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              uuid            NULL,
    updated_by              uuid            NULL,
    CONSTRAINT pk_courses PRIMARY KEY (course_id),
    CONSTRAINT ck_courses_total_hours CHECK (total_hours > 0)
);

CREATE INDEX ix_courses_tenant ON courses (tenant_id) WHERE is_deleted = false AND is_active = true;

-- ----------------------------------------------------------------------------

CREATE TABLE modules (
    module_id           uuid            NOT NULL DEFAULT uuid_generate_v4(),
    course_id           uuid            NOT NULL,
    sequence_order      integer         NOT NULL,
    title               varchar(300)    NOT NULL,
    description         text            NULL,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_modules PRIMARY KEY (module_id),
    CONSTRAINT fk_modules_course FOREIGN KEY (course_id)
        REFERENCES courses (course_id) ON DELETE CASCADE,
    CONSTRAINT uq_modules_course_sequence UNIQUE (course_id, sequence_order),
    CONSTRAINT ck_modules_sequence CHECK (sequence_order > 0)
);

-- ----------------------------------------------------------------------------

CREATE TABLE lessons (
    lesson_id               uuid            NOT NULL DEFAULT uuid_generate_v4(),
    module_id               uuid            NOT NULL,
    sequence_order          integer         NOT NULL,
    title                   varchar(300)    NOT NULL,
    min_duration_mins       integer         NOT NULL,
    requires_instructor     boolean         NOT NULL DEFAULT TRUE,
    is_mandatory            boolean         NOT NULL DEFAULT true,
    is_deleted              boolean         NOT NULL DEFAULT false,
    created_at              timestamptz     NOT NULL DEFAULT now(),
    updated_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              uuid            NULL,
    updated_by              uuid            NULL,
    CONSTRAINT pk_lessons PRIMARY KEY (lesson_id),
    CONSTRAINT fk_lessons_module FOREIGN KEY (module_id)
        REFERENCES modules (module_id) ON DELETE CASCADE,
    CONSTRAINT uq_lessons_module_sequence UNIQUE (module_id, sequence_order),
    CONSTRAINT ck_lessons_sequence CHECK (sequence_order > 0),
    CONSTRAINT ck_lessons_min_duration CHECK (min_duration_mins > 0)
);

-- ----------------------------------------------------------------------------

CREATE TABLE enrolments (
    enrolment_id        uuid                NOT NULL DEFAULT uuid_generate_v4(),
    user_id             uuid                NOT NULL,
    course_id           uuid                NOT NULL,
    org_id              uuid                NULL,
    status              enrolment_status    NOT NULL DEFAULT 'Active',
    enrolled_at         timestamptz         NOT NULL DEFAULT now(),
    completed_at        timestamptz         NULL,
    is_deleted          boolean             NOT NULL DEFAULT false,
    created_at          timestamptz         NOT NULL DEFAULT now(),
    updated_at          timestamptz         NOT NULL DEFAULT now(),
    created_by          uuid                NULL,
    updated_by          uuid                NULL,
    CONSTRAINT pk_enrolments PRIMARY KEY (enrolment_id),
    CONSTRAINT fk_enrolments_user FOREIGN KEY (user_id)
        REFERENCES app_users (user_id),
    CONSTRAINT fk_enrolments_course FOREIGN KEY (course_id)
        REFERENCES courses (course_id),
    CONSTRAINT fk_enrolments_org FOREIGN KEY (org_id)
        REFERENCES organisations (org_id) ON DELETE SET NULL,
    CONSTRAINT uq_enrolments_user_course UNIQUE (user_id, course_id)
);

CREATE INDEX ix_enrolments_user ON enrolments (user_id) WHERE is_deleted = false;
CREATE INDEX ix_enrolments_course ON enrolments (course_id) WHERE is_deleted = false;

-- ----------------------------------------------------------------------------

CREATE TABLE progress_records (
    progress_record_id  uuid            NOT NULL DEFAULT uuid_generate_v4(),
    enrolment_id        uuid            NOT NULL,
    lesson_id           uuid            NOT NULL,
    completed_at        timestamptz     NOT NULL DEFAULT now(),
    grade               varchar(20)     NULL,
    instructor_id       uuid            NULL,
    notes               text            NULL,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_progress_records PRIMARY KEY (progress_record_id),
    CONSTRAINT fk_progress_records_enrolment FOREIGN KEY (enrolment_id)
        REFERENCES enrolments (enrolment_id) ON DELETE CASCADE,
    CONSTRAINT fk_progress_records_lesson FOREIGN KEY (lesson_id)
        REFERENCES lessons (lesson_id),
    CONSTRAINT fk_progress_records_instructor FOREIGN KEY (instructor_id)
        REFERENCES app_users (user_id) ON DELETE SET NULL
);

CREATE INDEX ix_progress_records_enrolment ON progress_records (enrolment_id) WHERE is_deleted = false;

-- =============================================================================
-- INSTRUCTORS
-- =============================================================================

CREATE TABLE instructors (
    instructor_id           uuid            NOT NULL DEFAULT uuid_generate_v4(),
    user_id                 uuid            NOT NULL,
    employee_number         varchar(50)     NOT NULL,
    training_type_ratings   training_type[] NOT NULL,
    max_hours_per_week      smallint        NOT NULL DEFAULT 40,
    hire_date               date            NOT NULL,
    is_deleted              boolean         NOT NULL DEFAULT false,
    created_at              timestamptz     NOT NULL DEFAULT now(),
    updated_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              uuid            NULL,
    updated_by              uuid            NULL,
    CONSTRAINT pk_instructors PRIMARY KEY (instructor_id),
    CONSTRAINT fk_instructors_user FOREIGN KEY (user_id)
        REFERENCES app_users (user_id) ON DELETE CASCADE,
    CONSTRAINT uq_instructors_user UNIQUE (user_id),
    CONSTRAINT uq_instructors_employee_number UNIQUE (employee_number),
    CONSTRAINT ck_instructors_hours CHECK (max_hours_per_week > 0 AND max_hours_per_week <= 168),
    CONSTRAINT ck_instructors_ratings CHECK (array_length(training_type_ratings, 1) >= 1)
);

-- ----------------------------------------------------------------------------

CREATE TABLE instructor_availabilities (
    avail_id            uuid                NOT NULL DEFAULT uuid_generate_v4(),
    instructor_id       uuid                NOT NULL,
    start_at            timestamptz         NOT NULL,
    end_at              timestamptz         NOT NULL,
    avail_type          availability_type   NOT NULL,
    notes               text                NULL,
    is_deleted          boolean             NOT NULL DEFAULT false,
    created_at          timestamptz         NOT NULL DEFAULT now(),
    updated_at          timestamptz         NOT NULL DEFAULT now(),
    created_by          uuid                NULL,
    updated_by          uuid                NULL,
    CONSTRAINT pk_instructor_availabilities PRIMARY KEY (avail_id),
    CONSTRAINT fk_instructor_availabilities_instructor FOREIGN KEY (instructor_id)
        REFERENCES instructors (instructor_id) ON DELETE CASCADE,
    CONSTRAINT ck_instructor_availability_range CHECK (end_at > start_at)
);

CREATE INDEX ix_instructor_availabilities_range ON instructor_availabilities (instructor_id, start_at, end_at)
    WHERE is_deleted = false;

-- =============================================================================
-- BOOKINGS
-- =============================================================================

CREATE TABLE bookings (
    booking_id          uuid            NOT NULL DEFAULT uuid_generate_v4(),
    enrolment_id        uuid            NULL,
    config_id           uuid            NOT NULL,
    training_type       training_type   NOT NULL,
    student_count       integer         NOT NULL DEFAULT 1,
    status              booking_status  NOT NULL DEFAULT 'Provisional',
    booker_role         app_role        NOT NULL,
    booked_by           uuid            NOT NULL,
    org_id              uuid            NULL,
    -- Internal student fields (required when booked_by role = InternalStudent)
    department_name     character varying(200) NULL,
    budget_code         character varying(100) NULL,
    -- Pricing snapshot (locked at Confirmed; never recalculated after)
    gross_price_gbp     numeric(12,2)   NULL,
    discount_pct        numeric(5,2)    NULL,
    net_price_gbp       numeric(12,2)   NULL,
    -- Idempotency
    idempotency_key     uuid            NOT NULL,
    -- Provisional expiry (NULL for PendingApproval and beyond)
    provisional_expires_at timestamptz NULL,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_bookings PRIMARY KEY (booking_id),
    CONSTRAINT fk_bookings_enrolment FOREIGN KEY (enrolment_id)
        REFERENCES enrolments (enrolment_id) ON DELETE SET NULL,
    CONSTRAINT fk_bookings_config FOREIGN KEY (config_id)
        REFERENCES simulator_configurations (config_id),
    CONSTRAINT fk_bookings_booked_by FOREIGN KEY (booked_by)
        REFERENCES app_users (user_id),
    CONSTRAINT fk_bookings_org FOREIGN KEY (org_id)
        REFERENCES organisations (org_id) ON DELETE SET NULL,
    CONSTRAINT uq_bookings_idempotency_key UNIQUE (idempotency_key),
    CONSTRAINT ck_bookings_fd_capacity
        CHECK (training_type != 'flight_deck' OR student_count <= 4),
    CONSTRAINT ck_bookings_cc_capacity
        CHECK (training_type != 'cabin_crew' OR student_count <= 10),
    CONSTRAINT ck_bookings_student_count CHECK (student_count >= 1),
    CONSTRAINT ck_bookings_discount_pct CHECK (discount_pct >= 0 AND discount_pct <= 100),
    CONSTRAINT ck_bookings_price CHECK (
        (gross_price_gbp IS NULL AND net_price_gbp IS NULL)
        OR (gross_price_gbp >= 0 AND net_price_gbp >= 0)
    )
);

COMMENT ON CONSTRAINT ck_bookings_fd_capacity ON bookings
    IS 'Flight Deck bookings are capped at 4 students per session';
COMMENT ON CONSTRAINT ck_bookings_cc_capacity ON bookings
    IS 'Cabin Crew bookings are capped at 10 students per session';

CREATE INDEX ix_bookings_status ON bookings (status) WHERE is_deleted = false;
CREATE INDEX ix_bookings_booked_by ON bookings (booked_by) WHERE is_deleted = false;
CREATE INDEX ix_bookings_org_id ON bookings (org_id) WHERE is_deleted = false AND org_id IS NOT NULL;
CREATE INDEX ix_bookings_pending_approval ON bookings (created_at DESC)
    WHERE status = 'PendingApproval' AND is_deleted = false;

-- ----------------------------------------------------------------------------

CREATE TABLE booking_approvals (
    approval_id         uuid                NOT NULL DEFAULT uuid_generate_v4(),
    booking_id          uuid                NOT NULL,
    requested_at        timestamptz         NOT NULL DEFAULT now(),
    requested_by        uuid                NOT NULL,
    reviewed_at         timestamptz         NULL,
    reviewed_by         uuid                NULL,
    decision            approval_decision   NOT NULL DEFAULT 'Pending',
    rejection_reason    text                NULL,
    -- Immutable snapshot of values at submission time
    department_name     character varying(200) NULL,
    budget_code         character varying(100) NULL,
    created_at          timestamptz         NOT NULL DEFAULT now(),
    updated_at          timestamptz         NOT NULL DEFAULT now(),
    created_by          uuid                NULL,
    updated_by          uuid                NULL,
    CONSTRAINT pk_booking_approvals PRIMARY KEY (approval_id),
    CONSTRAINT fk_booking_approvals_booking FOREIGN KEY (booking_id)
        REFERENCES bookings (booking_id) ON DELETE CASCADE,
    CONSTRAINT fk_booking_approvals_requested_by FOREIGN KEY (requested_by)
        REFERENCES app_users (user_id),
    CONSTRAINT fk_booking_approvals_reviewed_by FOREIGN KEY (reviewed_by)
        REFERENCES app_users (user_id) ON DELETE SET NULL,
    CONSTRAINT uq_booking_approvals_booking UNIQUE (booking_id),
    CONSTRAINT ck_booking_approvals_no_self_approval CHECK (requested_by != reviewed_by),
    CONSTRAINT ck_booking_approvals_rejection CHECK (
        (decision = 'Rejected' AND rejection_reason IS NOT NULL AND char_length(rejection_reason) >= 10)
        OR decision != 'Rejected'
    ),
    CONSTRAINT ck_booking_approvals_reviewed CHECK (
        (decision != 'Pending' AND reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL)
        OR decision = 'Pending'
    )
);

COMMENT ON CONSTRAINT ck_booking_approvals_no_self_approval ON booking_approvals
    IS 'A reviewer cannot approve or reject their own booking';
COMMENT ON CONSTRAINT ck_booking_approvals_rejection ON booking_approvals
    IS 'Rejection reason is mandatory and must be at least 10 characters';

-- ----------------------------------------------------------------------------

CREATE TABLE booking_slots (
    slot_id             uuid            NOT NULL DEFAULT uuid_generate_v4(),
    booking_id          uuid            NOT NULL,
    bay_id              uuid            NOT NULL,
    start_at            timestamptz     NOT NULL,
    end_at              timestamptz     NOT NULL,
    duration_mins       integer         NOT NULL,
    instructor_id       uuid            NULL,
    lesson_id           uuid            NULL,
    slot_status         slot_status     NOT NULL DEFAULT 'Scheduled',
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_booking_slots PRIMARY KEY (slot_id),
    CONSTRAINT fk_booking_slots_booking FOREIGN KEY (booking_id)
        REFERENCES bookings (booking_id) ON DELETE CASCADE,
    CONSTRAINT fk_booking_slots_bay FOREIGN KEY (bay_id)
        REFERENCES simulator_bays (bay_id),
    CONSTRAINT fk_booking_slots_instructor FOREIGN KEY (instructor_id)
        REFERENCES instructors (instructor_id) ON DELETE SET NULL,
    CONSTRAINT fk_booking_slots_lesson FOREIGN KEY (lesson_id)
        REFERENCES lessons (lesson_id) ON DELETE SET NULL,
    CONSTRAINT ck_booking_slots_min_duration
        CHECK (duration_mins >= 240),
    CONSTRAINT ck_booking_slots_range CHECK (end_at > start_at)
);

COMMENT ON CONSTRAINT ck_booking_slots_min_duration ON booking_slots
    IS 'Minimum booking duration is 4 hours (240 minutes)';

-- Prevent double-booking: no two non-cancelled slots can overlap on the same bay
CREATE UNIQUE INDEX uq_booking_slots_bay_time
    ON booking_slots (bay_id, start_at, end_at)
    WHERE slot_status != 'Cancelled' AND is_deleted = false;

CREATE INDEX ix_booking_slots_booking_id ON booking_slots (booking_id) WHERE is_deleted = false;
CREATE INDEX ix_booking_slots_time_range ON booking_slots (start_at, end_at) WHERE is_deleted = false;

-- ----------------------------------------------------------------------------

CREATE TABLE reconfiguration_slots (
    reconfig_slot_id    uuid            NOT NULL DEFAULT uuid_generate_v4(),
    bay_id              uuid            NOT NULL,
    preceding_booking_id uuid           NULL,  -- NULL if first booking of the day
    to_booking_id       uuid            NULL,  -- NULL if last booking of the day
    start_at            timestamptz     NOT NULL,
    end_at              timestamptz     NOT NULL,
    duration_mins       integer         NOT NULL,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_reconfiguration_slots PRIMARY KEY (reconfig_slot_id),
    CONSTRAINT fk_reconfiguration_slots_bay FOREIGN KEY (bay_id)
        REFERENCES simulator_bays (bay_id),
    CONSTRAINT fk_reconfiguration_slots_from_booking FOREIGN KEY (preceding_booking_id)
        REFERENCES bookings (booking_id) ON DELETE SET NULL,
    CONSTRAINT fk_reconfiguration_slots_to_booking FOREIGN KEY (to_booking_id)
        REFERENCES bookings (booking_id) ON DELETE SET NULL,
    CONSTRAINT ck_reconfiguration_slots_range CHECK (end_at > start_at),
    CONSTRAINT ck_reconfiguration_slots_duration CHECK (duration_mins > 0)
);

-- No two reconfig slots can overlap on the same bay
CREATE UNIQUE INDEX uq_reconfiguration_slots_no_overlap
    ON reconfiguration_slots (bay_id, start_at);

CREATE INDEX ix_reconfiguration_slots_bay_range ON reconfiguration_slots (bay_id, start_at, end_at);

-- ----------------------------------------------------------------------------

CREATE TABLE booking_discounts (
    discount_id         uuid            NOT NULL DEFAULT uuid_generate_v4(),
    booking_id          uuid            NOT NULL,
    discount_rule_id    uuid            NULL,  -- NULL for manual/staff-rate discounts
    discount_type       varchar(50)     NOT NULL,
    discount_pct        numeric(5,2)    NOT NULL,
    amount_gbp          numeric(12,2)   NOT NULL,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    -- Immutable: no updated_at, no is_deleted
    CONSTRAINT pk_booking_discounts PRIMARY KEY (discount_id),
    CONSTRAINT fk_booking_discounts_discount_type FOREIGN KEY (discount_type) REFERENCES discount_types (code) ON DELETE RESTRICT,
    CONSTRAINT fk_booking_discounts_booking FOREIGN KEY (booking_id)
        REFERENCES bookings (booking_id) ON DELETE CASCADE,
    CONSTRAINT fk_booking_discounts_rule FOREIGN KEY (discount_rule_id)
        REFERENCES discount_rules (discount_rule_id) ON DELETE SET NULL,
    CONSTRAINT ck_booking_discounts_pct CHECK (discount_pct >= 0 AND discount_pct <= 100),
    CONSTRAINT ck_booking_discounts_amount CHECK (amount_gbp >= 0)
);

COMMENT ON TABLE booking_discounts
    IS 'Immutable audit snapshot of discounts applied at booking confirmation. Never updated.';

-- ----------------------------------------------------------------------------

CREATE TABLE booking_notes (
    note_id             uuid            NOT NULL DEFAULT uuid_generate_v4(),
    booking_id          uuid            NOT NULL,
    author_id           uuid            NOT NULL,
    content             text            NOT NULL,
    is_internal         boolean         NOT NULL DEFAULT TRUE,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_booking_notes PRIMARY KEY (note_id),
    CONSTRAINT fk_booking_notes_booking FOREIGN KEY (booking_id)
        REFERENCES bookings (booking_id) ON DELETE CASCADE,
    CONSTRAINT fk_booking_notes_author FOREIGN KEY (author_id)
        REFERENCES app_users (user_id)
);

CREATE INDEX ix_booking_notes_booking_id ON booking_notes (booking_id) WHERE is_deleted = false;

-- =============================================================================
-- INVOICES
-- =============================================================================

CREATE TABLE invoices (
    invoice_id          uuid            NOT NULL DEFAULT uuid_generate_v4(),
    booking_id          uuid            NOT NULL,
    org_id              uuid            NOT NULL,
    user_id             uuid            NOT NULL,
    issued_date         date            NOT NULL,
    gross_gbp           numeric(12,2)   NOT NULL,
    discount_gbp        numeric(12,2)   NOT NULL DEFAULT 0,
    net_gbp             numeric(12,2)   NOT NULL,
    status              invoice_status  NOT NULL DEFAULT 'Draft',
    due_date            date            NULL,
    paid_at             timestamptz     NULL,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_invoices PRIMARY KEY (invoice_id),
    CONSTRAINT fk_invoices_booking FOREIGN KEY (booking_id)
        REFERENCES bookings (booking_id),
    CONSTRAINT fk_invoices_org FOREIGN KEY (org_id)
        REFERENCES organisations (org_id) ON DELETE SET NULL,
    CONSTRAINT fk_invoices_user FOREIGN KEY (user_id)
        REFERENCES app_users (user_id),
    CONSTRAINT uq_invoices_booking UNIQUE (booking_id),
    CONSTRAINT ck_invoices_amounts CHECK (gross_gbp >= 0 AND net_gbp >= 0 AND discount_gbp >= 0),
    CONSTRAINT ck_invoices_net CHECK (net_gbp = gross_gbp - discount_gbp)
);

CREATE INDEX ix_invoices_org_id ON invoices (org_id) WHERE is_deleted = false AND org_id IS NOT NULL;
CREATE INDEX ix_invoices_status ON invoices (status) WHERE is_deleted = false;

-- Now add the deferred FK from payment_allocations to invoices
ALTER TABLE payment_allocations
    ADD CONSTRAINT fk_payment_allocations_invoice FOREIGN KEY (invoice_id)
        REFERENCES invoices (invoice_id) ON DELETE CASCADE;

CREATE INDEX ix_payment_allocations_invoice_id ON payment_allocations (invoice_id);

-- =============================================================================
-- REPORTS
-- =============================================================================

CREATE TABLE reports (
    report_id           uuid            NOT NULL DEFAULT uuid_generate_v4(),
    name                varchar(300)    NOT NULL,
    description         text            NULL,
    definition_json     jsonb           NOT NULL,
    owner_id            uuid            NOT NULL,
    is_shared           boolean         NOT NULL DEFAULT false,
    schedule_cron       varchar(100)    NULL,
    last_run_at         timestamptz     NULL,
    is_deleted          boolean         NOT NULL DEFAULT false,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    updated_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          uuid            NULL,
    updated_by          uuid            NULL,
    CONSTRAINT pk_reports PRIMARY KEY (report_id),
    CONSTRAINT fk_reports_owner FOREIGN KEY (owner_id)
        REFERENCES app_users (user_id)
);

-- ----------------------------------------------------------------------------

CREATE TABLE report_runs (
    run_id              uuid                NOT NULL DEFAULT uuid_generate_v4(),
    report_id           uuid                NOT NULL,
    status              report_run_status   NOT NULL DEFAULT 'Queued',
    started_at          timestamptz         NULL,
    completed_at        timestamptz         NULL,
    result_s3key        varchar(500)        NULL,
    error_message       character varying(2000) NULL,
    triggered_by        uuid                NOT NULL,
    created_at          timestamptz         NOT NULL DEFAULT now(),
    updated_at          timestamptz         NOT NULL DEFAULT now(),
    CONSTRAINT pk_report_runs PRIMARY KEY (run_id),
    CONSTRAINT fk_report_runs_report FOREIGN KEY (report_id)
        REFERENCES reports (report_id) ON DELETE CASCADE,
    CONSTRAINT fk_report_runs_triggered_by FOREIGN KEY (triggered_by)
        REFERENCES app_users (user_id)
);

CREATE INDEX ix_report_runs_report_id ON report_runs (report_id);

-- =============================================================================
-- ADDITIONAL UNIQUE INDEXES (aligned with EF-generated schema)
-- =============================================================================

-- app_users
CREATE UNIQUE INDEX uq_app_users_cognito_sub ON app_users (cognito_sub);
CREATE UNIQUE INDEX uq_app_users_email ON app_users (email);

-- bookings
CREATE UNIQUE INDEX uq_bookings_idempotency_key ON bookings (idempotency_key);

-- booking_approvals (performance — one approval record per booking)
CREATE UNIQUE INDEX ix_booking_approvals_booking_id ON booking_approvals (booking_id);

-- enrolments
CREATE UNIQUE INDEX uq_enrolments_user_course ON enrolments (user_id, course_id);

-- instructors
CREATE UNIQUE INDEX uq_instructors_employee_number ON instructors (employee_number);
CREATE UNIQUE INDEX uq_instructors_user ON instructors (user_id);

-- invitations
CREATE UNIQUE INDEX uq_invitations_token_hash ON invitations (token_hash);

-- lessons / modules
CREATE UNIQUE INDEX uq_lessons_module_sequence ON lessons (module_id, sequence_order);
CREATE UNIQUE INDEX uq_modules_course_sequence ON modules (course_id, sequence_order);

-- org_accounts (performance — one account per org)
CREATE UNIQUE INDEX ix_org_accounts_org_id ON org_accounts (org_id);

-- org_memberships
CREATE UNIQUE INDEX uq_org_memberships_user_org ON org_memberships (user_id, org_id);

-- reconfiguration_slots
CREATE UNIQUE INDEX uq_reconfig_slots_bay_time ON reconfiguration_slots (bay_id, start_at);

-- reconfiguration_templates
CREATE UNIQUE INDEX uq_reconfig_templates_pair ON reconfiguration_templates (from_config_id, to_config_id);

-- simulator_bays
CREATE UNIQUE INDEX uq_simulator_bays_code ON simulator_bays (simulator_unit_id, bay_code);

-- =============================================================================
-- ACCOUNT BALANCE TRIGGER
-- Maintains org_accounts.current_balance_gbp automatically.
-- Nightly reconciliation Lambda cross-checks this value against a full SUM query.
-- =============================================================================

CREATE OR REPLACE FUNCTION fsbs.update_org_balance()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE fsbs.org_accounts oa
    SET current_balance_gbp = (
        -- Total outstanding (issued/overdue) invoices
        SELECT COALESCE(SUM(i.net_gbp), 0)
        FROM fsbs.invoices i
        INNER JOIN fsbs.bookings b ON b.booking_id = i.booking_id
        WHERE b.org_id = oa.org_id
          AND i.status IN ('Issued', 'Overdue')
          AND i.is_deleted = false
    ) - (
        -- Total verified payments
        SELECT COALESCE(SUM(p.amount_gbp), 0)
        FROM fsbs.account_payments p
        WHERE p.org_id = oa.org_id
          AND p.status = 'Verified'
          AND p.is_deleted = false
    ),
    updated_at = now()
    WHERE oa.org_id = (
        CASE
            WHEN TG_TABLE_NAME = 'invoices' THEN
                (SELECT b.org_id FROM fsbs.bookings b WHERE b.booking_id = COALESCE(NEW.booking_id, OLD.booking_id))
            ELSE
                COALESCE(NEW.org_id, OLD.org_id)
        END
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_invoices_update_balance
    AFTER INSERT OR UPDATE OR DELETE ON fsbs.invoices
    FOR EACH ROW EXECUTE FUNCTION fsbs.update_org_balance();

CREATE TRIGGER trg_payments_update_balance
    AFTER INSERT OR UPDATE OR DELETE ON fsbs.account_payments
    FOR EACH ROW EXECUTE FUNCTION fsbs.update_org_balance();

-- =============================================================================
-- ACCOUNT STATEMENT TABLE
-- =============================================================================

CREATE TABLE account_statements (
    statement_id        uuid            NOT NULL DEFAULT uuid_generate_v4(),
    org_id              uuid            NOT NULL,
    period_start        date            NOT NULL,
    period_end          date            NOT NULL,
    opening_balance_gbp numeric(12,2)   NOT NULL,
    closing_balance_gbp numeric(12,2)   NOT NULL,
    statement_s3key     varchar(500)    NOT NULL,
    generated_at        timestamptz     NOT NULL DEFAULT now(),
    generated_by        uuid            NOT NULL,
    CONSTRAINT pk_account_statements PRIMARY KEY (statement_id),
    CONSTRAINT fk_account_statements_org FOREIGN KEY (org_id)
        REFERENCES organisations (org_id),
    CONSTRAINT fk_account_statements_generated_by FOREIGN KEY (generated_by)
        REFERENCES app_users (user_id),
    CONSTRAINT ck_account_statements_period CHECK (period_end >= period_start)
);

-- =============================================================================
-- ROW-LEVEL SECURITY (tenant isolation)
-- Enable on all tenant-scoped tables. The application sets app.current_tenant_id
-- at connection time via SET LOCAL, which EF Core middleware injects from the JWT.
-- =============================================================================

ALTER TABLE bookings         ENABLE ROW LEVEL SECURITY;
ALTER TABLE enrolments       ENABLE ROW LEVEL SECURITY;
ALTER TABLE courses          ENABLE ROW LEVEL SECURITY;
ALTER TABLE organisations    ENABLE ROW LEVEL SECURITY;
ALTER TABLE invitations      ENABLE ROW LEVEL SECURITY;
ALTER TABLE invoices         ENABLE ROW LEVEL SECURITY;

-- Staff (root tenant) bypass RLS for management operations.
-- Application service accounts use a role that bypasses RLS.
-- Customer-facing API connections use a restricted role subject to RLS.

-- Example policy (replicate pattern for each table):
CREATE POLICY tenant_isolation ON fsbs.organisations
    USING (tenant_id = current_setting('app.current_tenant_id')::uuid);

-- =============================================================================
-- GRANTS
-- =============================================================================

-- API application role (ECS task IAM → DB user via Secrets Manager)
-- CREATE ROLE fsbs_app LOGIN PASSWORD '...managed by Secrets Manager...';
-- GRANT USAGE ON SCHEMA fsbs TO fsbs_app;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA fsbs TO fsbs_app;
-- GRANT USAGE ON ALL SEQUENCES IN SCHEMA fsbs TO fsbs_app;

-- Read-only role for reporting / Management dashboards
-- CREATE ROLE fsbs_readonly;
-- GRANT USAGE ON SCHEMA fsbs TO fsbs_readonly;
-- GRANT SELECT ON ALL TABLES IN SCHEMA fsbs TO fsbs_readonly;

-- =============================================================================
-- END OF DDL
-- =============================================================================
