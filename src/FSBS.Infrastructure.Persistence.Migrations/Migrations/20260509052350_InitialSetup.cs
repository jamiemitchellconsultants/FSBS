using System;
using System.Collections.Generic;
using FSBS.Domain.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSBS.Infrastructure.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fsbs");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:fsbs.training_type", "flight_deck,cabin_crew");

            migrationBuilder.CreateTable(
                name: "app_users",
                schema: "fsbs",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cognito_sub = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    app_role = table.Column<string>(type: "text", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                schema: "fsbs",
                columns: table => new
                {
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    regulatory_framework = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    total_hours = table.Column<decimal>(type: "numeric(6,1)", precision: 6, scale: 1, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    training_type = table.Column<TrainingType>(type: "fsbs.training_type", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courses", x => x.course_id);
                    table.CheckConstraint("ck_courses_total_hours", "total_hours > 0");
                });

            migrationBuilder.CreateTable(
                name: "organisations",
                schema: "fsbs",
                columns: table => new
                {
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_class = table.Column<string>(type: "text", nullable: false),
                    contract_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    credit_limit_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    billing_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organisations", x => x.org_id);
                    table.CheckConstraint("ck_organisations_credit_limit", "credit_limit_gbp >= 0");
                });

            migrationBuilder.CreateTable(
                name: "instructors",
                schema: "fsbs",
                columns: table => new
                {
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employee_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    max_hours_per_week = table.Column<short>(type: "smallint", nullable: false),
                    hire_date = table.Column<DateOnly>(type: "date", nullable: false),
                    training_type_ratings = table.Column<List<TrainingType>>(type: "fsbs.training_type[]", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instructors", x => x.instructor_id);
                    table.CheckConstraint("ck_instructors_hours", "max_hours_per_week > 0");
                    table.ForeignKey(
                        name: "fk_instructors_app_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "fsbs",
                        principalTable: "app_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qualifications",
                schema: "fsbs",
                columns: table => new
                {
                    qualification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    issued_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    document_s3key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qualifications", x => x.qualification_id);
                    table.ForeignKey(
                        name: "fk_qualifications_app_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "fsbs",
                        principalTable: "app_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_qualifications_app_users_verified_by",
                        column: x => x.verified_by,
                        principalSchema: "fsbs",
                        principalTable: "app_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "fsbs",
                columns: table => new
                {
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    definition_json = table.Column<string>(type: "jsonb", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_shared = table.Column<bool>(type: "boolean", nullable: false),
                    schedule_cron = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.report_id);
                    table.ForeignKey(
                        name: "fk_reports_app_users_owner_id",
                        column: x => x.owner_id,
                        principalSchema: "fsbs",
                        principalTable: "app_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "fsbs",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profiles", x => x.user_id);
                    table.ForeignKey(
                        name: "fk_user_profiles_app_users_id",
                        column: x => x.user_id,
                        principalSchema: "fsbs",
                        principalTable: "app_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "modules",
                schema: "fsbs",
                columns: table => new
                {
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    sequence_order = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modules", x => x.module_id);
                    table.CheckConstraint("ck_modules_sequence", "sequence_order >= 1");
                    table.ForeignKey(
                        name: "fk_modules_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "fsbs",
                        principalTable: "courses",
                        principalColumn: "course_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "enrolments",
                schema: "fsbs",
                columns: table => new
                {
                    enrolment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: true),
                    enrolled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_enrolments", x => x.enrolment_id);
                    table.ForeignKey(
                        name: "fk_enrolments_app_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "fsbs",
                        principalTable: "app_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_enrolments_courses_course_id",
                        column: x => x.course_id,
                        principalSchema: "fsbs",
                        principalTable: "courses",
                        principalColumn: "course_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_enrolments_organisations_org_id",
                        column: x => x.org_id,
                        principalSchema: "fsbs",
                        principalTable: "organisations",
                        principalColumn: "org_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invitations",
                schema: "fsbs",
                columns: table => new
                {
                    invitation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitee_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    invitee_role = table.Column<string>(type: "text", nullable: false),
                    token_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    claimed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by = table.Column<Guid>(type: "uuid", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitations", x => x.invitation_id);
                    table.CheckConstraint("ck_invitations_claimed", "(status != 'Claimed' OR (claimed_by IS NOT NULL AND claimed_at IS NOT NULL))");
                    table.CheckConstraint("ck_invitations_revoked", "(status != 'Revoked' OR (revoked_by IS NOT NULL AND revoked_at IS NOT NULL))");
                    table.ForeignKey(
                        name: "fk_invitations_organisations_org_id",
                        column: x => x.org_id,
                        principalSchema: "fsbs",
                        principalTable: "organisations",
                        principalColumn: "org_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "org_accounts",
                schema: "fsbs",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_limit_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    current_balance_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    payment_terms_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_accounts", x => x.account_id);
                    table.CheckConstraint("ck_org_accounts_payment_terms", "payment_terms_days > 0");
                    table.ForeignKey(
                        name: "fk_org_accounts_organisations_org_id",
                        column: x => x.org_id,
                        principalSchema: "fsbs",
                        principalTable: "organisations",
                        principalColumn: "org_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "org_memberships",
                schema: "fsbs",
                columns: table => new
                {
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_role = table.Column<string>(type: "text", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_org_memberships", x => x.membership_id);
                    table.ForeignKey(
                        name: "fk_org_memberships_app_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "fsbs",
                        principalTable: "app_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_org_memberships_organisations_org_id",
                        column: x => x.org_id,
                        principalSchema: "fsbs",
                        principalTable: "organisations",
                        principalColumn: "org_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "instructor_availabilities",
                schema: "fsbs",
                columns: table => new
                {
                    avail_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    avail_type = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instructor_availabilities", x => x.avail_id);
                    table.CheckConstraint("ck_instructor_availability_range", "end_at > start_at");
                    table.ForeignKey(
                        name: "fk_instructor_availabilities_instructors_instructor_id",
                        column: x => x.instructor_id,
                        principalSchema: "fsbs",
                        principalTable: "instructors",
                        principalColumn: "instructor_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_runs",
                schema: "fsbs",
                columns: table => new
                {
                    run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    triggered_by = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    result_s3key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_runs", x => x.run_id);
                    table.ForeignKey(
                        name: "fk_report_runs_app_users_triggered_by",
                        column: x => x.triggered_by,
                        principalSchema: "fsbs",
                        principalTable: "app_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_report_runs_reports_report_id",
                        column: x => x.report_id,
                        principalSchema: "fsbs",
                        principalTable: "reports",
                        principalColumn: "report_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lessons",
                schema: "fsbs",
                columns: table => new
                {
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    sequence_order = table.Column<int>(type: "integer", nullable: false),
                    min_duration_mins = table.Column<int>(type: "integer", nullable: false),
                    requires_instructor = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lessons", x => x.lesson_id);
                    table.CheckConstraint("ck_lessons_sequence", "sequence_order >= 1");
                    table.ForeignKey(
                        name: "fk_lessons_modules_module_id",
                        column: x => x.module_id,
                        principalSchema: "fsbs",
                        principalTable: "modules",
                        principalColumn: "module_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_payments",
                schema: "fsbs",
                columns: table => new
                {
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_method = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    void_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_payments", x => x.payment_id);
                    table.CheckConstraint("ck_account_payments_amount", "amount_gbp > 0");
                    table.ForeignKey(
                        name: "fk_account_payments_app_users_recorded_by",
                        column: x => x.recorded_by,
                        principalSchema: "fsbs",
                        principalTable: "app_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_account_payments_org_accounts_org_account_id",
                        column: x => x.org_account_id,
                        principalSchema: "fsbs",
                        principalTable: "org_accounts",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_account_payments_organisations_org_id",
                        column: x => x.org_id,
                        principalSchema: "fsbs",
                        principalTable: "organisations",
                        principalColumn: "org_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "account_statements",
                schema: "fsbs",
                columns: table => new
                {
                    statement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    generated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    opening_balance_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    closing_balance_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    statement_s3key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    org_account_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_statements", x => x.statement_id);
                    table.ForeignKey(
                        name: "fk_account_statements_org_accounts_org_account_id",
                        column: x => x.org_account_id,
                        principalSchema: "fsbs",
                        principalTable: "org_accounts",
                        principalColumn: "account_id");
                    table.ForeignKey(
                        name: "fk_account_statements_organisations_org_id",
                        column: x => x.org_id,
                        principalSchema: "fsbs",
                        principalTable: "organisations",
                        principalColumn: "org_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "progress_records",
                schema: "fsbs",
                columns: table => new
                {
                    progress_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrolment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_progress_records", x => x.progress_record_id);
                    table.ForeignKey(
                        name: "fk_progress_records_enrolments_enrolment_id",
                        column: x => x.enrolment_id,
                        principalSchema: "fsbs",
                        principalTable: "enrolments",
                        principalColumn: "enrolment_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_progress_records_instructors_instructor_id",
                        column: x => x.instructor_id,
                        principalSchema: "fsbs",
                        principalTable: "instructors",
                        principalColumn: "instructor_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_progress_records_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalSchema: "fsbs",
                        principalTable: "lessons",
                        principalColumn: "lesson_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_approvals",
                schema: "fsbs",
                columns: table => new
                {
                    approval_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    department_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    budget_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decision = table.Column<string>(type: "text", nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_approvals", x => x.approval_id);
                    table.CheckConstraint("ck_booking_approvals_no_self_approval", "requested_by != reviewed_by");
                    table.CheckConstraint("ck_booking_approvals_rejection", "decision != 'Rejected' OR (rejection_reason IS NOT NULL AND char_length(rejection_reason) >= 10)");
                    table.CheckConstraint("ck_booking_approvals_reviewed", "decision = 'Pending' OR (reviewed_by IS NOT NULL AND reviewed_at IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "booking_discounts",
                schema: "fsbs",
                columns: table => new
                {
                    discount_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    discount_type = table.Column<string>(type: "text", nullable: false),
                    discount_pct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    amount_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_discounts", x => x.discount_id);
                    table.CheckConstraint("ck_booking_discounts_amount", "amount_gbp >= 0");
                    table.CheckConstraint("ck_booking_discounts_pct", "discount_pct >= 0 AND discount_pct <= 100");
                });

            migrationBuilder.CreateTable(
                name: "booking_notes",
                schema: "fsbs",
                columns: table => new
                {
                    note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    is_internal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_notes", x => x.note_id);
                    table.ForeignKey(
                        name: "fk_booking_notes_app_users_author_id",
                        column: x => x.author_id,
                        principalSchema: "fsbs",
                        principalTable: "app_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking_slots",
                schema: "fsbs",
                columns: table => new
                {
                    slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    instructor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_mins = table.Column<int>(type: "integer", nullable: false),
                    lesson_id = table.Column<Guid>(type: "uuid", nullable: true),
                    slot_status = table.Column<string>(type: "text", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_slots", x => x.slot_id);
                    table.CheckConstraint("ck_booking_slots_min_duration", "duration_mins >= 240");
                    table.CheckConstraint("ck_booking_slots_range", "end_at > start_at");
                    table.ForeignKey(
                        name: "fk_booking_slots_instructors_instructor_id",
                        column: x => x.instructor_id,
                        principalSchema: "fsbs",
                        principalTable: "instructors",
                        principalColumn: "instructor_id");
                    table.ForeignKey(
                        name: "fk_booking_slots_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalSchema: "fsbs",
                        principalTable: "lessons",
                        principalColumn: "lesson_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                schema: "fsbs",
                columns: table => new
                {
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booked_by = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: true),
                    booker_role = table.Column<string>(type: "text", nullable: false),
                    training_type = table.Column<TrainingType>(type: "fsbs.training_type", nullable: false),
                    config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrolment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    student_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    gross_price_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    discount_pct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    net_price_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    department_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    budget_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    idempotency_key = table.Column<Guid>(type: "uuid", nullable: false),
                    provisional_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.booking_id);
                    table.CheckConstraint("ck_bookings_cc_capacity", "training_type != 'cabin_crew' OR student_count <= 10");
                    table.CheckConstraint("ck_bookings_discount_pct", "discount_pct IS NULL OR (discount_pct >= 0 AND discount_pct <= 100)");
                    table.CheckConstraint("ck_bookings_fd_capacity", "training_type != 'flight_deck' OR student_count <= 4");
                    table.CheckConstraint("ck_bookings_price", "gross_price_gbp IS NULL OR gross_price_gbp >= 0");
                    table.CheckConstraint("ck_bookings_student_count", "student_count >= 1");
                    table.ForeignKey(
                        name: "fk_bookings_enrolments_enrolment_id",
                        column: x => x.enrolment_id,
                        principalSchema: "fsbs",
                        principalTable: "enrolments",
                        principalColumn: "enrolment_id");
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                schema: "fsbs",
                columns: table => new
                {
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    org_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    gross_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    discount_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    net_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    issued_date = table.Column<DateOnly>(type: "date", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.invoice_id);
                    table.CheckConstraint("ck_invoices_amounts", "gross_gbp >= 0 AND discount_gbp >= 0 AND net_gbp >= 0");
                    table.CheckConstraint("ck_invoices_net", "net_gbp = gross_gbp - discount_gbp");
                    table.ForeignKey(
                        name: "fk_invoices_app_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "fsbs",
                        principalTable: "app_users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invoices_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "fsbs",
                        principalTable: "bookings",
                        principalColumn: "booking_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_invoices_organisations_org_id",
                        column: x => x.org_id,
                        principalSchema: "fsbs",
                        principalTable: "organisations",
                        principalColumn: "org_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                schema: "fsbs",
                columns: table => new
                {
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_gbp = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_allocations", x => x.allocation_id);
                    table.ForeignKey(
                        name: "fk_payment_allocations_account_payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "fsbs",
                        principalTable: "account_payments",
                        principalColumn: "payment_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_payment_allocations_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalSchema: "fsbs",
                        principalTable: "invoices",
                        principalColumn: "invoice_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "discount_rules",
                schema: "fsbs",
                columns: table => new
                {
                    discount_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricing_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_type = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    discount_pct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_combinable = table.Column<bool>(type: "boolean", nullable: false),
                    threshold_json = table.Column<string>(type: "jsonb", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_rules", x => x.discount_rule_id);
                    table.CheckConstraint("ck_discount_rules_pct", "discount_pct >= 0 AND discount_pct <= 100");
                });

            migrationBuilder.CreateTable(
                name: "maintenance_windows",
                schema: "fsbs",
                columns: table => new
                {
                    maintenance_window_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenance_windows", x => x.maintenance_window_id);
                    table.CheckConstraint("ck_maintenance_windows_range", "end_at > start_at");
                });

            migrationBuilder.CreateTable(
                name: "pricing_policies",
                schema: "fsbs",
                columns: table => new
                {
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    training_type = table.Column<TrainingType>(type: "fsbs.training_type", nullable: false),
                    customer_class = table.Column<string>(type: "text", nullable: false),
                    hourly_rate_gbp = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pricing_policies", x => x.policy_id);
                });

            migrationBuilder.CreateTable(
                name: "reconfiguration_slots",
                schema: "fsbs",
                columns: table => new
                {
                    reconfig_slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    preceding_booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_mins = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reconfiguration_slots", x => x.reconfig_slot_id);
                    table.ForeignKey(
                        name: "fk_reconfiguration_slots_bookings_preceding_booking_id",
                        column: x => x.preceding_booking_id,
                        principalSchema: "fsbs",
                        principalTable: "bookings",
                        principalColumn: "booking_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reconfiguration_slots_bookings_to_booking_id",
                        column: x => x.to_booking_id,
                        principalSchema: "fsbs",
                        principalTable: "bookings",
                        principalColumn: "booking_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reconfiguration_templates",
                schema: "fsbs",
                columns: table => new
                {
                    reconfig_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duration_mins = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reconfiguration_templates", x => x.reconfig_template_id);
                    table.CheckConstraint("ck_reconfig_templates_different", "from_config_id != to_config_id");
                });

            migrationBuilder.CreateTable(
                name: "schedule_templates",
                schema: "fsbs",
                columns: table => new
                {
                    schedule_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    open_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    close_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_schedule_templates", x => x.schedule_template_id);
                });

            migrationBuilder.CreateTable(
                name: "simulator_bays",
                schema: "fsbs",
                columns: table => new
                {
                    bay_id = table.Column<Guid>(type: "uuid", nullable: false),
                    simulator_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bay_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulator_bays", x => x.bay_id);
                });

            migrationBuilder.CreateTable(
                name: "simulator_configurations",
                schema: "fsbs",
                columns: table => new
                {
                    config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    simulator_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aircraft_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    config_mode = table.Column<string>(type: "text", nullable: false),
                    supported_training_types = table.Column<List<TrainingType>>(type: "fsbs.training_type[]", nullable: false),
                    max_capacity_flight_deck = table.Column<int>(type: "integer", nullable: false),
                    max_capacity_cabin_crew = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulator_configurations", x => x.config_id);
                    table.CheckConstraint("ck_simulator_config_cc_capacity", "max_capacity_cabin_crew > 0 AND max_capacity_cabin_crew <= 10");
                    table.CheckConstraint("ck_simulator_config_fd_capacity", "max_capacity_flight_deck > 0 AND max_capacity_flight_deck <= 4");
                    table.CheckConstraint("ck_simulator_config_training_types", "array_length(supported_training_types, 1) >= 1");
                });

            migrationBuilder.CreateTable(
                name: "simulator_units",
                schema: "fsbs",
                columns: table => new
                {
                    unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    fstd_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    manufacturer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    active_configuration_id = table.Column<Guid>(type: "uuid", nullable: true),
                    default_reconfig_mins = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulator_units", x => x.unit_id);
                    table.CheckConstraint("ck_simulator_units_reconfig_mins", "default_reconfig_mins > 0");
                    table.ForeignKey(
                        name: "fk_simulator_units_simulator_configurations_active_configurati",
                        column: x => x.active_configuration_id,
                        principalSchema: "fsbs",
                        principalTable: "simulator_configurations",
                        principalColumn: "config_id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_account_payments_org_account_id",
                schema: "fsbs",
                table: "account_payments",
                column: "org_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_payments_org_id",
                schema: "fsbs",
                table: "account_payments",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_payments_recorded_by",
                schema: "fsbs",
                table: "account_payments",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "ix_account_statements_org_account_id",
                schema: "fsbs",
                table: "account_statements",
                column: "org_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_statements_org_id",
                schema: "fsbs",
                table: "account_statements",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "uq_app_users_cognito_sub",
                schema: "fsbs",
                table: "app_users",
                column: "cognito_sub",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_app_users_email",
                schema: "fsbs",
                table: "app_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_approvals_booking_id",
                schema: "fsbs",
                table: "booking_approvals",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_discounts_booking_id",
                schema: "fsbs",
                table: "booking_discounts",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_discounts_discount_rule_id",
                schema: "fsbs",
                table: "booking_discounts",
                column: "discount_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_notes_author_id",
                schema: "fsbs",
                table: "booking_notes",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_notes_booking_id",
                schema: "fsbs",
                table: "booking_notes",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_slots_booking_id",
                schema: "fsbs",
                table: "booking_slots",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_slots_instructor_id",
                schema: "fsbs",
                table: "booking_slots",
                column: "instructor_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_slots_lesson_id",
                schema: "fsbs",
                table: "booking_slots",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "uq_booking_slots_bay_time",
                schema: "fsbs",
                table: "booking_slots",
                columns: new[] { "bay_id", "start_at", "end_at" },
                unique: true,
                filter: "slot_status != 'Cancelled'");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_config_id",
                schema: "fsbs",
                table: "bookings",
                column: "config_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_enrolment_id",
                schema: "fsbs",
                table: "bookings",
                column: "enrolment_id");

            migrationBuilder.CreateIndex(
                name: "uq_bookings_idempotency_key",
                schema: "fsbs",
                table: "bookings",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_discount_rules_pricing_policy_id",
                schema: "fsbs",
                table: "discount_rules",
                column: "pricing_policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_enrolments_course_id",
                schema: "fsbs",
                table: "enrolments",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "ix_enrolments_org_id",
                schema: "fsbs",
                table: "enrolments",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "uq_enrolments_user_course",
                schema: "fsbs",
                table: "enrolments",
                columns: new[] { "user_id", "course_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_instructor_availabilities_instructor_id",
                schema: "fsbs",
                table: "instructor_availabilities",
                column: "instructor_id");

            migrationBuilder.CreateIndex(
                name: "uq_instructors_employee_number",
                schema: "fsbs",
                table: "instructors",
                column: "employee_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_instructors_user",
                schema: "fsbs",
                table: "instructors",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitations_org_id",
                schema: "fsbs",
                table: "invitations",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "uq_invitations_pending_email_org",
                schema: "fsbs",
                table: "invitations",
                columns: new[] { "invitee_email", "org_id" },
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "uq_invitations_token_hash",
                schema: "fsbs",
                table: "invitations",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_booking_id",
                schema: "fsbs",
                table: "invoices",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_org_id",
                schema: "fsbs",
                table: "invoices",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "ix_invoices_user_id",
                schema: "fsbs",
                table: "invoices",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_lessons_module_sequence",
                schema: "fsbs",
                table: "lessons",
                columns: new[] { "module_id", "sequence_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_windows_bay_id",
                schema: "fsbs",
                table: "maintenance_windows",
                column: "bay_id");

            migrationBuilder.CreateIndex(
                name: "uq_modules_course_sequence",
                schema: "fsbs",
                table: "modules",
                columns: new[] { "course_id", "sequence_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_org_accounts_org_id",
                schema: "fsbs",
                table: "org_accounts",
                column: "org_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_org_memberships_org_id",
                schema: "fsbs",
                table: "org_memberships",
                column: "org_id");

            migrationBuilder.CreateIndex(
                name: "uq_org_memberships_user_org",
                schema: "fsbs",
                table: "org_memberships",
                columns: new[] { "user_id", "org_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_invoice_id",
                schema: "fsbs",
                table: "payment_allocations",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_payment_id",
                schema: "fsbs",
                table: "payment_allocations",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_policies_configuration_id",
                schema: "fsbs",
                table: "pricing_policies",
                column: "configuration_id");

            migrationBuilder.CreateIndex(
                name: "ix_progress_records_enrolment_id",
                schema: "fsbs",
                table: "progress_records",
                column: "enrolment_id");

            migrationBuilder.CreateIndex(
                name: "ix_progress_records_instructor_id",
                schema: "fsbs",
                table: "progress_records",
                column: "instructor_id");

            migrationBuilder.CreateIndex(
                name: "ix_progress_records_lesson_id",
                schema: "fsbs",
                table: "progress_records",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "ix_qualifications_user_id",
                schema: "fsbs",
                table: "qualifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_qualifications_verified_by",
                schema: "fsbs",
                table: "qualifications",
                column: "verified_by");

            migrationBuilder.CreateIndex(
                name: "ix_reconfiguration_slots_preceding_booking_id",
                schema: "fsbs",
                table: "reconfiguration_slots",
                column: "preceding_booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_reconfiguration_slots_to_booking_id",
                schema: "fsbs",
                table: "reconfiguration_slots",
                column: "to_booking_id");

            migrationBuilder.CreateIndex(
                name: "uq_reconfig_slots_bay_time",
                schema: "fsbs",
                table: "reconfiguration_slots",
                columns: new[] { "bay_id", "start_at" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reconfiguration_templates_to_config_id",
                schema: "fsbs",
                table: "reconfiguration_templates",
                column: "to_config_id");

            migrationBuilder.CreateIndex(
                name: "uq_reconfig_templates_pair",
                schema: "fsbs",
                table: "reconfiguration_templates",
                columns: new[] { "from_config_id", "to_config_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_report_runs_report_id",
                schema: "fsbs",
                table: "report_runs",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_report_runs_triggered_by",
                schema: "fsbs",
                table: "report_runs",
                column: "triggered_by");

            migrationBuilder.CreateIndex(
                name: "ix_reports_owner_id",
                schema: "fsbs",
                table: "reports",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_templates_bay_id",
                schema: "fsbs",
                table: "schedule_templates",
                column: "bay_id");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_templates_config_id",
                schema: "fsbs",
                table: "schedule_templates",
                column: "config_id");

            migrationBuilder.CreateIndex(
                name: "uq_simulator_bays_code",
                schema: "fsbs",
                table: "simulator_bays",
                columns: new[] { "simulator_unit_id", "bay_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_simulator_configurations_simulator_unit_id",
                schema: "fsbs",
                table: "simulator_configurations",
                column: "simulator_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_simulator_units_active_configuration_id",
                schema: "fsbs",
                table: "simulator_units",
                column: "active_configuration_id");

            migrationBuilder.AddForeignKey(
                name: "fk_booking_approvals_bookings_booking_id",
                schema: "fsbs",
                table: "booking_approvals",
                column: "booking_id",
                principalSchema: "fsbs",
                principalTable: "bookings",
                principalColumn: "booking_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_discounts_bookings_booking_id",
                schema: "fsbs",
                table: "booking_discounts",
                column: "booking_id",
                principalSchema: "fsbs",
                principalTable: "bookings",
                principalColumn: "booking_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_discounts_discount_rules_discount_rule_id",
                schema: "fsbs",
                table: "booking_discounts",
                column: "discount_rule_id",
                principalSchema: "fsbs",
                principalTable: "discount_rules",
                principalColumn: "discount_rule_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_notes_bookings_booking_id",
                schema: "fsbs",
                table: "booking_notes",
                column: "booking_id",
                principalSchema: "fsbs",
                principalTable: "bookings",
                principalColumn: "booking_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slots_bookings_booking_id",
                schema: "fsbs",
                table: "booking_slots",
                column: "booking_id",
                principalSchema: "fsbs",
                principalTable: "bookings",
                principalColumn: "booking_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_slots_simulator_bays_bay_id",
                schema: "fsbs",
                table: "booking_slots",
                column: "bay_id",
                principalSchema: "fsbs",
                principalTable: "simulator_bays",
                principalColumn: "bay_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_bookings_simulator_configurations_config_id",
                schema: "fsbs",
                table: "bookings",
                column: "config_id",
                principalSchema: "fsbs",
                principalTable: "simulator_configurations",
                principalColumn: "config_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_rules_pricing_policies_pricing_policy_id",
                schema: "fsbs",
                table: "discount_rules",
                column: "pricing_policy_id",
                principalSchema: "fsbs",
                principalTable: "pricing_policies",
                principalColumn: "policy_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_maintenance_windows_simulator_bays_bay_id",
                schema: "fsbs",
                table: "maintenance_windows",
                column: "bay_id",
                principalSchema: "fsbs",
                principalTable: "simulator_bays",
                principalColumn: "bay_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pricing_policies_simulator_configurations_configuration_id",
                schema: "fsbs",
                table: "pricing_policies",
                column: "configuration_id",
                principalSchema: "fsbs",
                principalTable: "simulator_configurations",
                principalColumn: "config_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_reconfiguration_slots_simulator_bays_bay_id",
                schema: "fsbs",
                table: "reconfiguration_slots",
                column: "bay_id",
                principalSchema: "fsbs",
                principalTable: "simulator_bays",
                principalColumn: "bay_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_reconfiguration_templates_simulator_configurations_from_con",
                schema: "fsbs",
                table: "reconfiguration_templates",
                column: "from_config_id",
                principalSchema: "fsbs",
                principalTable: "simulator_configurations",
                principalColumn: "config_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_reconfiguration_templates_simulator_configurations_to_confi",
                schema: "fsbs",
                table: "reconfiguration_templates",
                column: "to_config_id",
                principalSchema: "fsbs",
                principalTable: "simulator_configurations",
                principalColumn: "config_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_schedule_templates_simulator_bays_bay_id",
                schema: "fsbs",
                table: "schedule_templates",
                column: "bay_id",
                principalSchema: "fsbs",
                principalTable: "simulator_bays",
                principalColumn: "bay_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_schedule_templates_simulator_configurations_config_id",
                schema: "fsbs",
                table: "schedule_templates",
                column: "config_id",
                principalSchema: "fsbs",
                principalTable: "simulator_configurations",
                principalColumn: "config_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_simulator_bays_simulator_units_simulator_unit_id",
                schema: "fsbs",
                table: "simulator_bays",
                column: "simulator_unit_id",
                principalSchema: "fsbs",
                principalTable: "simulator_units",
                principalColumn: "unit_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_simulator_configurations_simulator_units_simulator_unit_id",
                schema: "fsbs",
                table: "simulator_configurations",
                column: "simulator_unit_id",
                principalSchema: "fsbs",
                principalTable: "simulator_units",
                principalColumn: "unit_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_simulator_units_simulator_configurations_active_configurati",
                schema: "fsbs",
                table: "simulator_units");

            migrationBuilder.DropTable(
                name: "account_statements",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "booking_approvals",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "booking_discounts",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "booking_notes",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "booking_slots",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "instructor_availabilities",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "invitations",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "maintenance_windows",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "org_memberships",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "payment_allocations",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "progress_records",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "qualifications",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "reconfiguration_slots",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "reconfiguration_templates",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "report_runs",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "schedule_templates",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "discount_rules",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "account_payments",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "invoices",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "instructors",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "lessons",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "simulator_bays",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "pricing_policies",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "org_accounts",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "bookings",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "modules",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "enrolments",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "app_users",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "courses",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "organisations",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "simulator_configurations",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "simulator_units",
                schema: "fsbs");
        }
    }
}
