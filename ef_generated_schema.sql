DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'fsbs') THEN
        CREATE SCHEMA fsbs;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS fsbs.__ef_migrations_history (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'fsbs') THEN
            CREATE SCHEMA fsbs;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'fsbs') THEN
            CREATE SCHEMA fsbs;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TYPE fsbs.training_type AS ENUM ('flight_deck', 'cabin_crew');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.aircraft_types (
        aircraft_type_id uuid NOT NULL,
        icao_code character varying(20) NOT NULL,
        name character varying(100) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_aircraft_types PRIMARY KEY (aircraft_type_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.app_users (
        user_id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        cognito_sub character varying(128) NOT NULL,
        email character varying(256) NOT NULL,
        app_role text NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_app_users PRIMARY KEY (user_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.courses (
        course_id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        title character varying(300) NOT NULL,
        description text,
        regulatory_framework character varying(100),
        total_hours numeric(6,1) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        training_type fsbs.training_type NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_courses PRIMARY KEY (course_id),
        CONSTRAINT ck_courses_total_hours CHECK (total_hours > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.organisations (
        org_id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        customer_class text NOT NULL,
        contract_type character varying(50),
        credit_limit_gbp numeric(12,2) NOT NULL,
        billing_email character varying(255) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_organisations PRIMARY KEY (org_id),
        CONSTRAINT ck_organisations_credit_limit CHECK (credit_limit_gbp >= 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.instructors (
        instructor_id uuid NOT NULL,
        user_id uuid NOT NULL,
        employee_number character varying(50) NOT NULL,
        max_hours_per_week smallint NOT NULL,
        hire_date date NOT NULL,
        training_type_ratings fsbs.training_type[] NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_instructors PRIMARY KEY (instructor_id),
        CONSTRAINT ck_instructors_hours CHECK (max_hours_per_week > 0),
        CONSTRAINT fk_instructors_app_users_user_id FOREIGN KEY (user_id) REFERENCES fsbs.app_users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.qualifications (
        qualification_id uuid NOT NULL,
        user_id uuid NOT NULL,
        type character varying(100) NOT NULL,
        issued_date date NOT NULL,
        expiry_date date,
        document_s3key character varying(500),
        verified_by uuid,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_qualifications PRIMARY KEY (qualification_id),
        CONSTRAINT fk_qualifications_app_users_user_id FOREIGN KEY (user_id) REFERENCES fsbs.app_users (user_id) ON DELETE RESTRICT,
        CONSTRAINT fk_qualifications_app_users_verified_by FOREIGN KEY (verified_by) REFERENCES fsbs.app_users (user_id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.reports (
        report_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        description text,
        definition_json jsonb NOT NULL,
        owner_id uuid NOT NULL,
        is_shared boolean NOT NULL,
        schedule_cron character varying(100),
        last_run_at timestamp with time zone,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_reports PRIMARY KEY (report_id),
        CONSTRAINT fk_reports_app_users_owner_id FOREIGN KEY (owner_id) REFERENCES fsbs.app_users (user_id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.user_profiles (
        user_id uuid NOT NULL,
        first_name character varying(100) NOT NULL,
        last_name character varying(100) NOT NULL,
        phone_number character varying(30),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_user_profiles PRIMARY KEY (user_id),
        CONSTRAINT fk_user_profiles_app_users_id FOREIGN KEY (user_id) REFERENCES fsbs.app_users (user_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.modules (
        module_id uuid NOT NULL,
        course_id uuid NOT NULL,
        title character varying(300) NOT NULL,
        sequence_order integer NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_modules PRIMARY KEY (module_id),
        CONSTRAINT ck_modules_sequence CHECK (sequence_order >= 1),
        CONSTRAINT fk_modules_courses_course_id FOREIGN KEY (course_id) REFERENCES fsbs.courses (course_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.enrolments (
        enrolment_id uuid NOT NULL,
        user_id uuid NOT NULL,
        course_id uuid NOT NULL,
        org_id uuid,
        enrolled_at timestamp with time zone NOT NULL,
        status text NOT NULL,
        completed_at timestamp with time zone,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_enrolments PRIMARY KEY (enrolment_id),
        CONSTRAINT fk_enrolments_app_users_user_id FOREIGN KEY (user_id) REFERENCES fsbs.app_users (user_id) ON DELETE CASCADE,
        CONSTRAINT fk_enrolments_courses_course_id FOREIGN KEY (course_id) REFERENCES fsbs.courses (course_id) ON DELETE CASCADE,
        CONSTRAINT fk_enrolments_organisations_org_id FOREIGN KEY (org_id) REFERENCES fsbs.organisations (org_id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.invitations (
        invitation_id uuid NOT NULL,
        org_id uuid NOT NULL,
        invitee_email character varying(256) NOT NULL,
        invitee_role text NOT NULL,
        token_hash character(64) NOT NULL,
        status text NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        claimed_by uuid,
        claimed_at timestamp with time zone,
        revoked_by uuid,
        revoked_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_invitations PRIMARY KEY (invitation_id),
        CONSTRAINT ck_invitations_claimed CHECK ((status != 'Claimed' OR (claimed_by IS NOT NULL AND claimed_at IS NOT NULL))),
        CONSTRAINT ck_invitations_revoked CHECK ((status != 'Revoked' OR (revoked_by IS NOT NULL AND revoked_at IS NOT NULL))),
        CONSTRAINT fk_invitations_organisations_org_id FOREIGN KEY (org_id) REFERENCES fsbs.organisations (org_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.org_accounts (
        account_id uuid NOT NULL,
        org_id uuid NOT NULL,
        credit_limit_gbp numeric(12,2) NOT NULL,
        current_balance_gbp numeric(12,2) NOT NULL,
        status text NOT NULL,
        payment_terms_days integer NOT NULL DEFAULT 30,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_org_accounts PRIMARY KEY (account_id),
        CONSTRAINT ck_org_accounts_payment_terms CHECK (payment_terms_days > 0),
        CONSTRAINT fk_org_accounts_organisations_org_id FOREIGN KEY (org_id) REFERENCES fsbs.organisations (org_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.org_memberships (
        membership_id uuid NOT NULL,
        user_id uuid NOT NULL,
        org_id uuid NOT NULL,
        org_role text NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_org_memberships PRIMARY KEY (membership_id),
        CONSTRAINT fk_org_memberships_app_users_user_id FOREIGN KEY (user_id) REFERENCES fsbs.app_users (user_id) ON DELETE CASCADE,
        CONSTRAINT fk_org_memberships_organisations_org_id FOREIGN KEY (org_id) REFERENCES fsbs.organisations (org_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.instructor_availabilities (
        avail_id uuid NOT NULL,
        instructor_id uuid NOT NULL,
        start_at timestamp with time zone NOT NULL,
        end_at timestamp with time zone NOT NULL,
        avail_type text NOT NULL,
        notes text,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_instructor_availabilities PRIMARY KEY (avail_id),
        CONSTRAINT ck_instructor_availability_range CHECK (end_at > start_at),
        CONSTRAINT fk_instructor_availabilities_instructors_instructor_id FOREIGN KEY (instructor_id) REFERENCES fsbs.instructors (instructor_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.report_runs (
        run_id uuid NOT NULL,
        report_id uuid NOT NULL,
        triggered_by uuid NOT NULL,
        status text NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        started_at timestamp with time zone,
        completed_at timestamp with time zone,
        result_s3key character varying(500),
        error_message character varying(2000),
        CONSTRAINT pk_report_runs PRIMARY KEY (run_id),
        CONSTRAINT fk_report_runs_app_users_triggered_by FOREIGN KEY (triggered_by) REFERENCES fsbs.app_users (user_id) ON DELETE RESTRICT,
        CONSTRAINT fk_report_runs_reports_report_id FOREIGN KEY (report_id) REFERENCES fsbs.reports (report_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.lessons (
        lesson_id uuid NOT NULL,
        module_id uuid NOT NULL,
        title character varying(300) NOT NULL,
        sequence_order integer NOT NULL,
        min_duration_mins integer NOT NULL,
        requires_instructor boolean NOT NULL DEFAULT TRUE,
        is_mandatory boolean NOT NULL DEFAULT TRUE,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_lessons PRIMARY KEY (lesson_id),
        CONSTRAINT ck_lessons_sequence CHECK (sequence_order >= 1),
        CONSTRAINT fk_lessons_modules_module_id FOREIGN KEY (module_id) REFERENCES fsbs.modules (module_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.account_payments (
        payment_id uuid NOT NULL,
        org_account_id uuid NOT NULL,
        org_id uuid NOT NULL,
        amount_gbp numeric(12,2) NOT NULL,
        payment_date date NOT NULL,
        recorded_by uuid NOT NULL,
        payment_method text NOT NULL,
        status text NOT NULL,
        reference character varying(200),
        notes character varying(1000),
        verified_by uuid,
        verified_at timestamp with time zone,
        void_reason character varying(500),
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_account_payments PRIMARY KEY (payment_id),
        CONSTRAINT ck_account_payments_amount CHECK (amount_gbp > 0),
        CONSTRAINT fk_account_payments_app_users_recorded_by FOREIGN KEY (recorded_by) REFERENCES fsbs.app_users (user_id) ON DELETE RESTRICT,
        CONSTRAINT fk_account_payments_org_accounts_org_account_id FOREIGN KEY (org_account_id) REFERENCES fsbs.org_accounts (account_id) ON DELETE CASCADE,
        CONSTRAINT fk_account_payments_organisations_org_id FOREIGN KEY (org_id) REFERENCES fsbs.organisations (org_id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.account_statements (
        statement_id uuid NOT NULL,
        org_id uuid NOT NULL,
        generated_at timestamp with time zone NOT NULL,
        generated_by uuid NOT NULL,
        period_start date NOT NULL,
        period_end date NOT NULL,
        opening_balance_gbp numeric(12,2) NOT NULL,
        closing_balance_gbp numeric(12,2) NOT NULL,
        statement_s3key character varying(500) NOT NULL,
        org_account_id uuid,
        CONSTRAINT pk_account_statements PRIMARY KEY (statement_id),
        CONSTRAINT fk_account_statements_org_accounts_org_account_id FOREIGN KEY (org_account_id) REFERENCES fsbs.org_accounts (account_id),
        CONSTRAINT fk_account_statements_organisations_org_id FOREIGN KEY (org_id) REFERENCES fsbs.organisations (org_id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.progress_records (
        progress_record_id uuid NOT NULL,
        enrolment_id uuid NOT NULL,
        lesson_id uuid NOT NULL,
        completed_at timestamp with time zone NOT NULL,
        instructor_id uuid,
        grade character varying(20),
        notes text,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_progress_records PRIMARY KEY (progress_record_id),
        CONSTRAINT fk_progress_records_enrolments_enrolment_id FOREIGN KEY (enrolment_id) REFERENCES fsbs.enrolments (enrolment_id) ON DELETE CASCADE,
        CONSTRAINT fk_progress_records_instructors_instructor_id FOREIGN KEY (instructor_id) REFERENCES fsbs.instructors (instructor_id) ON DELETE RESTRICT,
        CONSTRAINT fk_progress_records_lessons_lesson_id FOREIGN KEY (lesson_id) REFERENCES fsbs.lessons (lesson_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.booking_approvals (
        approval_id uuid NOT NULL,
        booking_id uuid NOT NULL,
        requested_by uuid NOT NULL,
        requested_at timestamp with time zone NOT NULL,
        department_name character varying(200),
        budget_code character varying(100),
        reviewed_by uuid,
        reviewed_at timestamp with time zone,
        decision text NOT NULL,
        rejection_reason character varying(2000),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_booking_approvals PRIMARY KEY (approval_id),
        CONSTRAINT ck_booking_approvals_no_self_approval CHECK (requested_by != reviewed_by),
        CONSTRAINT ck_booking_approvals_rejection CHECK (decision != 'Rejected' OR (rejection_reason IS NOT NULL AND char_length(rejection_reason) >= 10)),
        CONSTRAINT ck_booking_approvals_reviewed CHECK (decision = 'Pending' OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.booking_discounts (
        discount_id uuid NOT NULL,
        booking_id uuid NOT NULL,
        discount_rule_id uuid,
        discount_type text NOT NULL,
        discount_pct numeric(5,2) NOT NULL,
        amount_gbp numeric(12,2) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_booking_discounts PRIMARY KEY (discount_id),
        CONSTRAINT ck_booking_discounts_amount CHECK (amount_gbp >= 0),
        CONSTRAINT ck_booking_discounts_pct CHECK (discount_pct >= 0 AND discount_pct <= 100)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.booking_notes (
        note_id uuid NOT NULL,
        booking_id uuid NOT NULL,
        author_id uuid NOT NULL,
        content text NOT NULL,
        is_internal boolean NOT NULL DEFAULT TRUE,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_booking_notes PRIMARY KEY (note_id),
        CONSTRAINT fk_booking_notes_app_users_author_id FOREIGN KEY (author_id) REFERENCES fsbs.app_users (user_id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.booking_slots (
        slot_id uuid NOT NULL,
        booking_id uuid NOT NULL,
        bay_id uuid NOT NULL,
        instructor_id uuid,
        start_at timestamp with time zone NOT NULL,
        end_at timestamp with time zone NOT NULL,
        duration_mins integer NOT NULL,
        lesson_id uuid,
        slot_status text NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_booking_slots PRIMARY KEY (slot_id),
        CONSTRAINT ck_booking_slots_min_duration CHECK (duration_mins >= 240),
        CONSTRAINT ck_booking_slots_range CHECK (end_at > start_at),
        CONSTRAINT fk_booking_slots_instructors_instructor_id FOREIGN KEY (instructor_id) REFERENCES fsbs.instructors (instructor_id),
        CONSTRAINT fk_booking_slots_lessons_lesson_id FOREIGN KEY (lesson_id) REFERENCES fsbs.lessons (lesson_id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.bookings (
        booking_id uuid NOT NULL,
        booked_by uuid NOT NULL,
        org_id uuid,
        booker_role text NOT NULL,
        training_type fsbs.training_type NOT NULL,
        config_id uuid NOT NULL,
        enrolment_id uuid,
        student_count integer NOT NULL,
        status text NOT NULL,
        gross_price_gbp numeric(12,2),
        discount_pct numeric(5,2),
        net_price_gbp numeric(12,2),
        department_name character varying(200),
        budget_code character varying(100),
        idempotency_key uuid NOT NULL,
        provisional_expires_at timestamp with time zone,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_bookings PRIMARY KEY (booking_id),
        CONSTRAINT ck_bookings_cc_capacity CHECK (training_type != 'cabin_crew' OR student_count <= 10),
        CONSTRAINT ck_bookings_discount_pct CHECK (discount_pct IS NULL OR (discount_pct >= 0 AND discount_pct <= 100)),
        CONSTRAINT ck_bookings_fd_capacity CHECK (training_type != 'flight_deck' OR student_count <= 4),
        CONSTRAINT ck_bookings_price CHECK (gross_price_gbp IS NULL OR gross_price_gbp >= 0),
        CONSTRAINT ck_bookings_student_count CHECK (student_count >= 1),
        CONSTRAINT fk_bookings_enrolments_enrolment_id FOREIGN KEY (enrolment_id) REFERENCES fsbs.enrolments (enrolment_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.invoices (
        invoice_id uuid NOT NULL,
        booking_id uuid NOT NULL,
        org_id uuid NOT NULL,
        user_id uuid NOT NULL,
        status text NOT NULL,
        gross_gbp numeric(12,2) NOT NULL,
        discount_gbp numeric(12,2) NOT NULL,
        net_gbp numeric(12,2) NOT NULL,
        issued_date date NOT NULL,
        due_date date,
        paid_at timestamp with time zone,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_invoices PRIMARY KEY (invoice_id),
        CONSTRAINT ck_invoices_amounts CHECK (gross_gbp >= 0 AND discount_gbp >= 0 AND net_gbp >= 0),
        CONSTRAINT ck_invoices_net CHECK (net_gbp = gross_gbp - discount_gbp),
        CONSTRAINT fk_invoices_app_users_user_id FOREIGN KEY (user_id) REFERENCES fsbs.app_users (user_id) ON DELETE RESTRICT,
        CONSTRAINT fk_invoices_bookings_booking_id FOREIGN KEY (booking_id) REFERENCES fsbs.bookings (booking_id) ON DELETE CASCADE,
        CONSTRAINT fk_invoices_organisations_org_id FOREIGN KEY (org_id) REFERENCES fsbs.organisations (org_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.payment_allocations (
        allocation_id uuid NOT NULL,
        payment_id uuid NOT NULL,
        invoice_id uuid NOT NULL,
        amount_gbp numeric(12,2) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_payment_allocations PRIMARY KEY (allocation_id),
        CONSTRAINT fk_payment_allocations_account_payments_payment_id FOREIGN KEY (payment_id) REFERENCES fsbs.account_payments (payment_id) ON DELETE CASCADE,
        CONSTRAINT fk_payment_allocations_invoices_invoice_id FOREIGN KEY (invoice_id) REFERENCES fsbs.invoices (invoice_id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.discount_rules (
        discount_rule_id uuid NOT NULL,
        pricing_policy_id uuid NOT NULL,
        discount_type text NOT NULL,
        priority integer NOT NULL,
        discount_pct numeric(5,2) NOT NULL,
        is_combinable boolean NOT NULL,
        threshold_json jsonb,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_discount_rules PRIMARY KEY (discount_rule_id),
        CONSTRAINT ck_discount_rules_pct CHECK (discount_pct >= 0 AND discount_pct <= 100)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.maintenance_windows (
        maintenance_window_id uuid NOT NULL,
        bay_id uuid NOT NULL,
        start_at timestamp with time zone NOT NULL,
        end_at timestamp with time zone NOT NULL,
        reason text NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_maintenance_windows PRIMARY KEY (maintenance_window_id),
        CONSTRAINT ck_maintenance_windows_range CHECK (end_at > start_at)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.pricing_policies (
        policy_id uuid NOT NULL,
        configuration_id uuid NOT NULL,
        training_type fsbs.training_type NOT NULL,
        customer_class text NOT NULL,
        hourly_rate_gbp numeric(10,2) NOT NULL,
        effective_from date NOT NULL,
        effective_to date,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_pricing_policies PRIMARY KEY (policy_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.reconfiguration_slots (
        reconfig_slot_id uuid NOT NULL,
        bay_id uuid NOT NULL,
        preceding_booking_id uuid,
        to_booking_id uuid,
        start_at timestamp with time zone NOT NULL,
        end_at timestamp with time zone NOT NULL,
        duration_mins integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_reconfiguration_slots PRIMARY KEY (reconfig_slot_id),
        CONSTRAINT fk_reconfiguration_slots_bookings_preceding_booking_id FOREIGN KEY (preceding_booking_id) REFERENCES fsbs.bookings (booking_id) ON DELETE RESTRICT,
        CONSTRAINT fk_reconfiguration_slots_bookings_to_booking_id FOREIGN KEY (to_booking_id) REFERENCES fsbs.bookings (booking_id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.reconfiguration_templates (
        reconfig_template_id uuid NOT NULL,
        from_config_id uuid NOT NULL,
        to_config_id uuid NOT NULL,
        duration_mins integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_reconfiguration_templates PRIMARY KEY (reconfig_template_id),
        CONSTRAINT ck_reconfig_templates_different CHECK (from_config_id != to_config_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.schedule_templates (
        schedule_template_id uuid NOT NULL,
        bay_id uuid NOT NULL,
        config_id uuid NOT NULL,
        day_of_week integer NOT NULL,
        open_time time without time zone NOT NULL,
        close_time time without time zone NOT NULL,
        valid_from date NOT NULL,
        valid_to date,
        is_active boolean NOT NULL DEFAULT TRUE,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_schedule_templates PRIMARY KEY (schedule_template_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.simulator_bays (
        bay_id uuid NOT NULL,
        simulator_unit_id uuid NOT NULL,
        bay_code character varying(20) NOT NULL,
        description text,
        status text NOT NULL,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_simulator_bays PRIMARY KEY (bay_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.simulator_configurations (
        config_id uuid NOT NULL,
        simulator_unit_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        aircraft_type_id uuid NOT NULL,
        config_mode text NOT NULL,
        supported_training_types fsbs.training_type[] NOT NULL,
        max_capacity_flight_deck integer NOT NULL,
        max_capacity_cabin_crew integer NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_simulator_configurations PRIMARY KEY (config_id),
        CONSTRAINT ck_simulator_config_cc_capacity CHECK (max_capacity_cabin_crew > 0 AND max_capacity_cabin_crew <= 10),
        CONSTRAINT ck_simulator_config_fd_capacity CHECK (max_capacity_flight_deck > 0 AND max_capacity_flight_deck <= 4),
        CONSTRAINT ck_simulator_config_training_types CHECK (array_length(supported_training_types, 1) >= 1),
        CONSTRAINT fk_simulator_configurations_aircraft_types_aircraft_type_id FOREIGN KEY (aircraft_type_id) REFERENCES fsbs.aircraft_types (aircraft_type_id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE TABLE fsbs.simulator_units (
        unit_id uuid NOT NULL,
        name character varying(200) NOT NULL,
        fstd_level character varying(20) NOT NULL,
        manufacturer character varying(100),
        location character varying(200),
        active_configuration_id uuid,
        default_reconfig_mins integer NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        is_deleted boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        created_by uuid,
        updated_by uuid,
        CONSTRAINT pk_simulator_units PRIMARY KEY (unit_id),
        CONSTRAINT ck_simulator_units_reconfig_mins CHECK (default_reconfig_mins > 0),
        CONSTRAINT fk_simulator_units_simulator_configurations_active_configurati FOREIGN KEY (active_configuration_id) REFERENCES fsbs.simulator_configurations (config_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_account_payments_org_account_id ON fsbs.account_payments (org_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_account_payments_org_id ON fsbs.account_payments (org_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_account_payments_recorded_by ON fsbs.account_payments (recorded_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_account_statements_org_account_id ON fsbs.account_statements (org_account_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_account_statements_org_id ON fsbs.account_statements (org_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_aircraft_types_icao_code ON fsbs.aircraft_types (icao_code) WHERE is_deleted = false;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_app_users_cognito_sub ON fsbs.app_users (cognito_sub);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_app_users_email ON fsbs.app_users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX ix_booking_approvals_booking_id ON fsbs.booking_approvals (booking_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_booking_discounts_booking_id ON fsbs.booking_discounts (booking_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_booking_discounts_discount_rule_id ON fsbs.booking_discounts (discount_rule_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_booking_notes_author_id ON fsbs.booking_notes (author_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_booking_notes_booking_id ON fsbs.booking_notes (booking_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_booking_slots_booking_id ON fsbs.booking_slots (booking_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_booking_slots_instructor_id ON fsbs.booking_slots (instructor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_booking_slots_lesson_id ON fsbs.booking_slots (lesson_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_booking_slots_bay_time ON fsbs.booking_slots (bay_id, start_at, end_at) WHERE slot_status != 'Cancelled';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_bookings_config_id ON fsbs.bookings (config_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_bookings_enrolment_id ON fsbs.bookings (enrolment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_bookings_idempotency_key ON fsbs.bookings (idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_discount_rules_pricing_policy_id ON fsbs.discount_rules (pricing_policy_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_enrolments_course_id ON fsbs.enrolments (course_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_enrolments_org_id ON fsbs.enrolments (org_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_enrolments_user_course ON fsbs.enrolments (user_id, course_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_instructor_availabilities_instructor_id ON fsbs.instructor_availabilities (instructor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_instructors_employee_number ON fsbs.instructors (employee_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_instructors_user ON fsbs.instructors (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_invitations_org_id ON fsbs.invitations (org_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_invitations_pending_email_org ON fsbs.invitations (invitee_email, org_id) WHERE status = 'Pending';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_invitations_token_hash ON fsbs.invitations (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_invoices_booking_id ON fsbs.invoices (booking_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_invoices_org_id ON fsbs.invoices (org_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_invoices_user_id ON fsbs.invoices (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_lessons_module_sequence ON fsbs.lessons (module_id, sequence_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_maintenance_windows_bay_id ON fsbs.maintenance_windows (bay_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_modules_course_sequence ON fsbs.modules (course_id, sequence_order);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX ix_org_accounts_org_id ON fsbs.org_accounts (org_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_org_memberships_org_id ON fsbs.org_memberships (org_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_org_memberships_user_org ON fsbs.org_memberships (user_id, org_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_payment_allocations_invoice_id ON fsbs.payment_allocations (invoice_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_payment_allocations_payment_id ON fsbs.payment_allocations (payment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_pricing_policies_configuration_id ON fsbs.pricing_policies (configuration_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_progress_records_enrolment_id ON fsbs.progress_records (enrolment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_progress_records_instructor_id ON fsbs.progress_records (instructor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_progress_records_lesson_id ON fsbs.progress_records (lesson_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_qualifications_user_id ON fsbs.qualifications (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_qualifications_verified_by ON fsbs.qualifications (verified_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_reconfiguration_slots_preceding_booking_id ON fsbs.reconfiguration_slots (preceding_booking_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_reconfiguration_slots_to_booking_id ON fsbs.reconfiguration_slots (to_booking_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_reconfig_slots_bay_time ON fsbs.reconfiguration_slots (bay_id, start_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_reconfiguration_templates_to_config_id ON fsbs.reconfiguration_templates (to_config_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_reconfig_templates_pair ON fsbs.reconfiguration_templates (from_config_id, to_config_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_report_runs_report_id ON fsbs.report_runs (report_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_report_runs_triggered_by ON fsbs.report_runs (triggered_by);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_reports_owner_id ON fsbs.reports (owner_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_schedule_templates_bay_id ON fsbs.schedule_templates (bay_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_schedule_templates_config_id ON fsbs.schedule_templates (config_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE UNIQUE INDEX uq_simulator_bays_code ON fsbs.simulator_bays (simulator_unit_id, bay_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_simulator_configurations_aircraft_type_id ON fsbs.simulator_configurations (aircraft_type_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_simulator_configurations_simulator_unit_id ON fsbs.simulator_configurations (simulator_unit_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    CREATE INDEX ix_simulator_units_active_configuration_id ON fsbs.simulator_units (active_configuration_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.booking_approvals ADD CONSTRAINT fk_booking_approvals_bookings_booking_id FOREIGN KEY (booking_id) REFERENCES fsbs.bookings (booking_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.booking_discounts ADD CONSTRAINT fk_booking_discounts_bookings_booking_id FOREIGN KEY (booking_id) REFERENCES fsbs.bookings (booking_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.booking_discounts ADD CONSTRAINT fk_booking_discounts_discount_rules_discount_rule_id FOREIGN KEY (discount_rule_id) REFERENCES fsbs.discount_rules (discount_rule_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.booking_notes ADD CONSTRAINT fk_booking_notes_bookings_booking_id FOREIGN KEY (booking_id) REFERENCES fsbs.bookings (booking_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.booking_slots ADD CONSTRAINT fk_booking_slots_bookings_booking_id FOREIGN KEY (booking_id) REFERENCES fsbs.bookings (booking_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.booking_slots ADD CONSTRAINT fk_booking_slots_simulator_bays_bay_id FOREIGN KEY (bay_id) REFERENCES fsbs.simulator_bays (bay_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.bookings ADD CONSTRAINT fk_bookings_simulator_configurations_config_id FOREIGN KEY (config_id) REFERENCES fsbs.simulator_configurations (config_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.discount_rules ADD CONSTRAINT fk_discount_rules_pricing_policies_pricing_policy_id FOREIGN KEY (pricing_policy_id) REFERENCES fsbs.pricing_policies (policy_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.maintenance_windows ADD CONSTRAINT fk_maintenance_windows_simulator_bays_bay_id FOREIGN KEY (bay_id) REFERENCES fsbs.simulator_bays (bay_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.pricing_policies ADD CONSTRAINT fk_pricing_policies_simulator_configurations_configuration_id FOREIGN KEY (configuration_id) REFERENCES fsbs.simulator_configurations (config_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.reconfiguration_slots ADD CONSTRAINT fk_reconfiguration_slots_simulator_bays_bay_id FOREIGN KEY (bay_id) REFERENCES fsbs.simulator_bays (bay_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.reconfiguration_templates ADD CONSTRAINT fk_reconfiguration_templates_simulator_configurations_from_con FOREIGN KEY (from_config_id) REFERENCES fsbs.simulator_configurations (config_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.reconfiguration_templates ADD CONSTRAINT fk_reconfiguration_templates_simulator_configurations_to_confi FOREIGN KEY (to_config_id) REFERENCES fsbs.simulator_configurations (config_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.schedule_templates ADD CONSTRAINT fk_schedule_templates_simulator_bays_bay_id FOREIGN KEY (bay_id) REFERENCES fsbs.simulator_bays (bay_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.schedule_templates ADD CONSTRAINT fk_schedule_templates_simulator_configurations_config_id FOREIGN KEY (config_id) REFERENCES fsbs.simulator_configurations (config_id) ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.simulator_bays ADD CONSTRAINT fk_simulator_bays_simulator_units_simulator_unit_id FOREIGN KEY (simulator_unit_id) REFERENCES fsbs.simulator_units (unit_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    ALTER TABLE fsbs.simulator_configurations ADD CONSTRAINT fk_simulator_configurations_simulator_units_simulator_unit_id FOREIGN KEY (simulator_unit_id) REFERENCES fsbs.simulator_units (unit_id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM fsbs.__ef_migrations_history WHERE "migration_id" = '20260509191156_InitialSetup') THEN
    INSERT INTO fsbs.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260509191156_InitialSetup', '10.0.7');
    END IF;
END $EF$;
COMMIT;

