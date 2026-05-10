using FSBS.Domain.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FSBS.Infrastructure.Persistence.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddNativeEnumsAndReferenceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid=t.typnamespace WHERE t.typname='invitation_status' AND n.nspname='fsbs') THEN CREATE TYPE fsbs.invitation_status AS ENUM ('pending','claimed','expired','revoked'); END IF; END $$;");
            migrationBuilder.Sql("DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid=t.typnamespace WHERE t.typname='invitee_role' AND n.nspname='fsbs') THEN CREATE TYPE fsbs.invitee_role AS ENUM ('corporate_manager','corporate_student'); END IF; END $$;");
            migrationBuilder.Sql("DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid=t.typnamespace WHERE t.typname='availability_type' AND n.nspname='fsbs') THEN CREATE TYPE fsbs.availability_type AS ENUM ('available','leave','other'); END IF; END $$;");
            migrationBuilder.Sql("DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid=t.typnamespace WHERE t.typname='bay_status' AND n.nspname='fsbs') THEN CREATE TYPE fsbs.bay_status AS ENUM ('operational','maintenance','decommissioned'); END IF; END $$;");
            migrationBuilder.Sql("DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid=t.typnamespace WHERE t.typname='org_role' AND n.nspname='fsbs') THEN CREATE TYPE fsbs.org_role AS ENUM ('manager','student'); END IF; END $$;");

            migrationBuilder.Sql("ALTER TABLE fsbs.simulator_bays ALTER COLUMN status TYPE fsbs.bay_status USING status::text::fsbs.bay_status;");
            migrationBuilder.Sql("ALTER TABLE fsbs.org_memberships ALTER COLUMN org_role TYPE fsbs.org_role USING org_role::text::fsbs.org_role;");
            // Drop text-based CHECK constraints before casting invitation columns to enum types
            migrationBuilder.Sql("ALTER TABLE fsbs.invitations DROP CONSTRAINT IF EXISTS ck_invitations_claimed;");
            migrationBuilder.Sql("ALTER TABLE fsbs.invitations DROP CONSTRAINT IF EXISTS ck_invitations_revoked;");
            // Drop partial index that references status as text
            migrationBuilder.Sql("DROP INDEX IF EXISTS fsbs.uq_invitations_pending_email_org;");
            migrationBuilder.Sql("ALTER TABLE fsbs.invitations ALTER COLUMN status TYPE fsbs.invitation_status USING status::text::fsbs.invitation_status;");
            migrationBuilder.Sql("ALTER TABLE fsbs.invitations ALTER COLUMN invitee_role TYPE fsbs.invitee_role USING (CASE invitee_role::text WHEN 'CorporateManager' THEN 'corporate_manager' WHEN 'CorporateStudent' THEN 'corporate_student' ELSE lower(invitee_role::text) END)::fsbs.invitee_role;");
            // Recreate CHECK constraints using enum-typed comparisons
            migrationBuilder.Sql("ALTER TABLE fsbs.invitations ADD CONSTRAINT ck_invitations_claimed CHECK ((status != 'claimed'::fsbs.invitation_status OR (claimed_by IS NOT NULL AND claimed_at IS NOT NULL)));");
            migrationBuilder.Sql("ALTER TABLE fsbs.invitations ADD CONSTRAINT ck_invitations_revoked CHECK ((status != 'revoked'::fsbs.invitation_status OR (revoked_by IS NOT NULL AND revoked_at IS NOT NULL)));");
            // Recreate partial index using enum-typed comparison
            migrationBuilder.Sql("CREATE UNIQUE INDEX uq_invitations_pending_email_org ON fsbs.invitations (invitee_email, org_id) WHERE status = 'pending'::fsbs.invitation_status;");
            migrationBuilder.Sql("ALTER TABLE fsbs.instructor_availabilities ALTER COLUMN avail_type TYPE fsbs.availability_type USING avail_type::text::fsbs.availability_type;");

            migrationBuilder.Sql("ALTER TABLE fsbs.pricing_policies ALTER COLUMN customer_class TYPE character varying(50);");
            migrationBuilder.Sql("ALTER TABLE fsbs.organisations ALTER COLUMN customer_class TYPE character varying(50);");
            migrationBuilder.Sql("ALTER TABLE fsbs.org_accounts ALTER COLUMN status TYPE character varying(50);");
            migrationBuilder.Sql("ALTER TABLE fsbs.discount_rules ALTER COLUMN discount_type TYPE character varying(50);");
            migrationBuilder.Sql("ALTER TABLE fsbs.booking_discounts ALTER COLUMN discount_type TYPE character varying(50);");
            migrationBuilder.Sql("ALTER TABLE fsbs.account_payments ALTER COLUMN status TYPE character varying(50);");
            migrationBuilder.Sql("ALTER TABLE fsbs.account_payments ALTER COLUMN payment_method TYPE character varying(50);");

            migrationBuilder.CreateTable(
                name: "account_statuses",
                schema: "fsbs",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    allows_booking = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_statuses", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "customer_classes",
                schema: "fsbs",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_classes", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "discount_types",
                schema: "fsbs",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discount_types", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "payment_methods",
                schema: "fsbs",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_methods", x => x.code);
                });

            migrationBuilder.InsertData(
                schema: "fsbs",
                table: "account_statuses",
                columns: new[] { "code", "allows_booking", "label" },
                values: new object[] { "Active", true, "Active" });

            migrationBuilder.InsertData(
                schema: "fsbs",
                table: "account_statuses",
                columns: new[] { "code", "label" },
                values: new object[,]
                {
                    { "Closed", "Closed" },
                    { "Suspended", "Suspended" }
                });

            migrationBuilder.InsertData(
                schema: "fsbs",
                table: "customer_classes",
                columns: new[] { "code", "is_active", "label" },
                values: new object[,]
                {
                    { "Corporate", true, "Corporate" },
                    { "Staff", true, "Staff" },
                    { "Standard", true, "Standard" }
                });

            migrationBuilder.InsertData(
                schema: "fsbs",
                table: "discount_types",
                columns: new[] { "code", "is_active", "label" },
                values: new object[,]
                {
                    { "AdvanceBooking", true, "Advance Booking" },
                    { "CorporateNegotiated", true, "Corporate Negotiated" },
                    { "Promotional", true, "Promotional" },
                    { "StaffRate", true, "Staff Rate" },
                    { "VolumeAdvanceBlock", true, "Volume Advance Block" },
                    { "VolumeOrgSession", true, "Volume Org Session" }
                });

            migrationBuilder.InsertData(
                schema: "fsbs",
                table: "payment_methods",
                columns: new[] { "code", "is_active", "label" },
                values: new object[,]
                {
                    { "Adjustment", true, "Adjustment" },
                    { "BankTransfer", true, "Bank Transfer" },
                    { "Cash", true, "Cash" },
                    { "Cheque", true, "Cheque" },
                    { "CreditNote", true, "Credit Note" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_pricing_policies_customer_class",
                schema: "fsbs",
                table: "pricing_policies",
                column: "customer_class");

            migrationBuilder.CreateIndex(
                name: "ix_organisations_customer_class",
                schema: "fsbs",
                table: "organisations",
                column: "customer_class");

            migrationBuilder.CreateIndex(
                name: "ix_org_accounts_status",
                schema: "fsbs",
                table: "org_accounts",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_discount_rules_discount_type",
                schema: "fsbs",
                table: "discount_rules",
                column: "discount_type");

            migrationBuilder.CreateIndex(
                name: "ix_booking_discounts_discount_type",
                schema: "fsbs",
                table: "booking_discounts",
                column: "discount_type");

            migrationBuilder.CreateIndex(
                name: "ix_account_payments_payment_method",
                schema: "fsbs",
                table: "account_payments",
                column: "payment_method");

            migrationBuilder.AddForeignKey(
                name: "fk_account_payments_payment_methods_payment_method",
                schema: "fsbs",
                table: "account_payments",
                column: "payment_method",
                principalSchema: "fsbs",
                principalTable: "payment_methods",
                principalColumn: "code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_discounts_discount_types_discount_type",
                schema: "fsbs",
                table: "booking_discounts",
                column: "discount_type",
                principalSchema: "fsbs",
                principalTable: "discount_types",
                principalColumn: "code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_discount_rules_discount_types_discount_type",
                schema: "fsbs",
                table: "discount_rules",
                column: "discount_type",
                principalSchema: "fsbs",
                principalTable: "discount_types",
                principalColumn: "code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_org_accounts_account_statuses_status",
                schema: "fsbs",
                table: "org_accounts",
                column: "status",
                principalSchema: "fsbs",
                principalTable: "account_statuses",
                principalColumn: "code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_organisations_customer_classes_customer_class",
                schema: "fsbs",
                table: "organisations",
                column: "customer_class",
                principalSchema: "fsbs",
                principalTable: "customer_classes",
                principalColumn: "code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pricing_policies_customer_classes_customer_class",
                schema: "fsbs",
                table: "pricing_policies",
                column: "customer_class",
                principalSchema: "fsbs",
                principalTable: "customer_classes",
                principalColumn: "code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_account_payments_payment_methods_payment_method",
                schema: "fsbs",
                table: "account_payments");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_discounts_discount_types_discount_type",
                schema: "fsbs",
                table: "booking_discounts");

            migrationBuilder.DropForeignKey(
                name: "fk_discount_rules_discount_types_discount_type",
                schema: "fsbs",
                table: "discount_rules");

            migrationBuilder.DropForeignKey(
                name: "fk_org_accounts_account_statuses_status",
                schema: "fsbs",
                table: "org_accounts");

            migrationBuilder.DropForeignKey(
                name: "fk_organisations_customer_classes_customer_class",
                schema: "fsbs",
                table: "organisations");

            migrationBuilder.DropForeignKey(
                name: "fk_pricing_policies_customer_classes_customer_class",
                schema: "fsbs",
                table: "pricing_policies");

            migrationBuilder.DropTable(
                name: "account_statuses",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "customer_classes",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "discount_types",
                schema: "fsbs");

            migrationBuilder.DropTable(
                name: "payment_methods",
                schema: "fsbs");

            migrationBuilder.DropIndex(
                name: "ix_pricing_policies_customer_class",
                schema: "fsbs",
                table: "pricing_policies");

            migrationBuilder.DropIndex(
                name: "ix_organisations_customer_class",
                schema: "fsbs",
                table: "organisations");

            migrationBuilder.DropIndex(
                name: "ix_org_accounts_status",
                schema: "fsbs",
                table: "org_accounts");

            migrationBuilder.DropIndex(
                name: "ix_discount_rules_discount_type",
                schema: "fsbs",
                table: "discount_rules");

            migrationBuilder.DropIndex(
                name: "ix_booking_discounts_discount_type",
                schema: "fsbs",
                table: "booking_discounts");

            migrationBuilder.DropIndex(
                name: "ix_account_payments_payment_method",
                schema: "fsbs",
                table: "account_payments");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:fsbs.training_type", "flight_deck,cabin_crew")
                .OldAnnotation("Npgsql:Enum:fsbs.availability_type", "available,leave,other")
                .OldAnnotation("Npgsql:Enum:fsbs.bay_status", "operational,maintenance,decommissioned")
                .OldAnnotation("Npgsql:Enum:fsbs.invitation_status", "pending,claimed,expired,revoked")
                .OldAnnotation("Npgsql:Enum:fsbs.invitee_role", "corporate_manager,corporate_student")
                .OldAnnotation("Npgsql:Enum:fsbs.org_role", "manager,student")
                .OldAnnotation("Npgsql:Enum:fsbs.training_type", "flight_deck,cabin_crew");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "fsbs",
                table: "simulator_bays",
                type: "text",
                nullable: false,
                oldClrType: typeof(BayStatus),
                oldType: "fsbs.bay_status");

            migrationBuilder.AlterColumn<string>(
                name: "customer_class",
                schema: "fsbs",
                table: "pricing_policies",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "customer_class",
                schema: "fsbs",
                table: "organisations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "org_role",
                schema: "fsbs",
                table: "org_memberships",
                type: "text",
                nullable: false,
                oldClrType: typeof(OrgRole),
                oldType: "fsbs.org_role");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "fsbs",
                table: "org_accounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "fsbs",
                table: "invitations",
                type: "text",
                nullable: false,
                oldClrType: typeof(InvitationStatus),
                oldType: "fsbs.invitation_status");

            migrationBuilder.AlterColumn<string>(
                name: "invitee_role",
                schema: "fsbs",
                table: "invitations",
                type: "text",
                nullable: false,
                oldClrType: typeof(InviteeRole),
                oldType: "fsbs.invitee_role");

            migrationBuilder.AlterColumn<string>(
                name: "avail_type",
                schema: "fsbs",
                table: "instructor_availabilities",
                type: "text",
                nullable: false,
                oldClrType: typeof(AvailabilityType),
                oldType: "fsbs.availability_type");

            migrationBuilder.AlterColumn<string>(
                name: "discount_type",
                schema: "fsbs",
                table: "discount_rules",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "discount_type",
                schema: "fsbs",
                table: "booking_discounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "fsbs",
                table: "account_payments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "payment_method",
                schema: "fsbs",
                table: "account_payments",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.Sql("DROP TYPE IF EXISTS fsbs.invitation_status;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS fsbs.invitee_role;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS fsbs.availability_type;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS fsbs.bay_status;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS fsbs.org_role;");
        }
    }
}
