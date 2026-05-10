using FSBS.Application.Common.Interfaces;
using FSBS.Domain.Entities;
using FSBS.Domain.Enums;
using FSBS.Infrastructure.Persistence.Entities.Configurations;
using FSBS.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace FSBS.Infrastructure.Persistence;

/// <summary>
/// The single EF Core <see cref="DbContext"/> for the FSBS application.
/// All tables live in the <c>fsbs</c> PostgreSQL schema; snake_case column
/// naming is applied globally via <c>UseSnakeCaseNamingConvention()</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Construction:</b> registered as a scoped service via
/// <see cref="PersistenceServiceExtensions.AddPersistence"/>. The primary
/// constructor receives <paramref name="currentUser"/> from DI — this is
/// captured as a field and referenced inside the global query filter lambdas,
/// meaning the tenant/soft-delete predicates are re-evaluated per request,
/// not compiled once at startup.
/// </para>
/// <para>
/// <b>Audit stamping:</b> <see cref="AuditInterceptor"/> (registered as a
/// singleton and attached via <c>AddInterceptors</c>) stamps
/// <c>CreatedAt</c>, <c>CreatedBy</c>, <c>UpdatedAt</c>, and <c>UpdatedBy</c>
/// on every <see cref="AuditableEntity"/> before <c>SaveChanges</c> completes.
/// </para>
/// <para>
/// <b>Optimistic concurrency:</b> every <see cref="AuditableEntity"/> table
/// exposes the PostgreSQL <c>xmin</c> system column as a concurrency token,
/// configured in each entity's <c>IEntityTypeConfiguration</c>.
/// </para>
/// <para>
/// <b>Migrations:</b> the migration assembly is
/// <c>FSBS.Infrastructure.Persistence.Migrations</c>. Never run
/// <c>dotnet ef database update</c> against production manually — migrations
/// are applied exclusively through the CI/CD pipeline.
/// </para>
/// </remarks>
public class FsbsDbContext(DbContextOptions<FsbsDbContext> options, ICurrentUser currentUser)
    : DbContext(options)
{
    // -------------------------------------------------------------------------
    // Identity & organisations
    // -------------------------------------------------------------------------

    /// <summary>
    /// All registered users across both Cognito pools (staff and customers).
    /// Filtered by <see cref="ICurrentUser.TenantId"/> and soft-delete at query time.
    /// Use <c>IgnoreQueryFilters()</c> when an admin operation must cross tenant boundaries.
    /// </summary>
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    /// <summary>
    /// Personal details for users. Shares the primary key column (<c>user_id</c>)
    /// with <c>app_users</c> — always accessed via <see cref="AppUser.Profile"/>.
    /// </summary>
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    /// <summary>Flight and cabin-crew qualifications used as reference data for instructor records.</summary>
    public DbSet<Qualification> Qualifications => Set<Qualification>();

    /// <summary>
    /// Corporate customer organisations. Filtered by <see cref="ICurrentUser.TenantId"/>
    /// so each organisation's data is invisible to other tenants.
    /// </summary>
    public DbSet<Organisation> Organisations => Set<Organisation>();

    /// <summary>Links between users and the organisations they belong to, with their intra-org role.</summary>
    public DbSet<OrgMembership> OrgMemberships => Set<OrgMembership>();

    /// <summary>
    /// Financial accounts for organisations. <c>CurrentBalanceGbp</c> on these
    /// rows is maintained by a PostgreSQL trigger — never written by EF Core directly.
    /// </summary>
    public DbSet<OrgAccount> OrgAccounts => Set<OrgAccount>();

    /// <summary>
    /// Payments posted against organisation accounts. Only <c>Verified</c> payments
    /// affect the running balance; <c>Pending</c> payments are held for Management approval.
    /// </summary>
    public DbSet<AccountPayment> AccountPayments => Set<AccountPayment>();

    /// <summary>Point-in-time JSON snapshots of an organisation's account statement. Immutable once created.</summary>
    public DbSet<AccountStatement> AccountStatements => Set<AccountStatement>();

    /// <summary>
    /// Invitation records for corporate customer registration. The raw token is never
    /// stored — only the SHA-256 hash in <c>token_hash</c>.
    /// </summary>
    public DbSet<Invitation> Invitations => Set<Invitation>();

    // -------------------------------------------------------------------------
    // Simulator infrastructure
    // -------------------------------------------------------------------------

    /// <summary>Physical simulator devices, each with a current active configuration and one or more bays.</summary>
    public DbSet<SimulatorUnit> SimulatorUnits => Set<SimulatorUnit>();

    /// <summary>Reference data for aircraft types that can be simulated (e.g. B737-800, A320).</summary>
    public DbSet<AircraftType> AircraftTypes => Set<AircraftType>();

    /// <summary>
    /// Aircraft-type and cabin-layout setups for a simulator unit. Drives pricing tier
    /// selection and enforces FlightDeck (max 4) and CabinCrew (max 10) capacity hard caps.
    /// </summary>
    public DbSet<SimulatorConfiguration> SimulatorConfigurations => Set<SimulatorConfiguration>();

    /// <summary>
    /// Individual bookable bays within a simulator unit. The unique index on
    /// <c>(bay_id, start_at, end_at)</c> filtered to non-cancelled slots prevents double-booking.
    /// </summary>
    public DbSet<SimulatorBay> SimulatorBays => Set<SimulatorBay>();

    /// <summary>Scheduled maintenance periods that block bay availability on the calendar.</summary>
    public DbSet<MaintenanceWindow> MaintenanceWindows => Set<MaintenanceWindow>();

    /// <summary>
    /// Turnaround durations for switching between configuration pairs. When no template
    /// exists for a pair the unit's <c>DefaultReconfigMins</c> is used as fallback.
    /// </summary>
    public DbSet<ReconfigurationTemplate> ReconfigurationTemplates => Set<ReconfigurationTemplate>();

    /// <summary>Reusable scheduling blueprints associated with a specific simulator configuration.</summary>
    public DbSet<ScheduleTemplate> ScheduleTemplates => Set<ScheduleTemplate>();

    // -------------------------------------------------------------------------
    // Pricing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Base hourly rates per (configuration, training type, customer class, effective date).
    /// The most recently effective policy matching the booking's key tuple is selected at
    /// confirmation time and the price is then locked permanently.
    /// </summary>
    public DbSet<PricingPolicy> PricingPolicies => Set<PricingPolicy>();

    /// <summary>
    /// Threshold-based discount rules attached to a pricing policy. Evaluated at booking
    /// confirmation and snapshotted as <see cref="BookingDiscounts"/>. Never applied to
    /// InternalStudent bookings.
    /// </summary>
    public DbSet<DiscountRule> DiscountRules => Set<DiscountRule>();

    // -------------------------------------------------------------------------
    // Training & progress
    // -------------------------------------------------------------------------

    /// <summary>
    /// Structured training programmes. Tenant-scoped; filtered by
    /// <see cref="ICurrentUser.TenantId"/> at query time.
    /// </summary>
    public DbSet<Course> Courses => Set<Course>();

    /// <summary>Logical chapters within a course, ordered by <c>sequence_order</c>.</summary>
    public DbSet<Module> Modules => Set<Module>();

    /// <summary>Individual deliverable sessions within a module.</summary>
    public DbSet<Lesson> Lessons => Set<Lesson>();

    /// <summary>
    /// Student enrolments on a course. A unique constraint on <c>(user_id, course_id)</c>
    /// prevents duplicate active enrolments.
    /// </summary>
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();

    /// <summary>Instructor or CourseDirector sign-offs confirming a student completed a lesson.</summary>
    public DbSet<ProgressRecord> ProgressRecords => Set<ProgressRecord>();

    /// <summary>
    /// Staff members authorised to deliver sessions. Assignment to a booking slot
    /// requires the instructor's <c>training_type_ratings</c> array to contain the
    /// booking's training type.
    /// </summary>
    public DbSet<Instructor> Instructors => Set<Instructor>();

    /// <summary>Declared availability and leave windows used to filter eligible instructors during scheduling.</summary>
    public DbSet<InstructorAvailability> InstructorAvailabilities => Set<InstructorAvailability>();

    // -------------------------------------------------------------------------
    // Bookings
    // -------------------------------------------------------------------------

    /// <summary>
    /// The central aggregate. Owns slots, approval, notes, and discount snapshots.
    /// InternalStudent bookings enter <c>PendingApproval</c>; all others enter
    /// <c>Provisional</c>. The <c>idempotency_key</c> unique index allows safe
    /// client retries on <c>POST /bookings</c>.
    /// </summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>
    /// Time blocks reserved in a simulator bay. Minimum duration of 240 minutes
    /// enforced by CHECK constraint. Filtered unique index prevents double-booking
    /// across non-cancelled slots.
    /// </summary>
    public DbSet<BookingSlot> BookingSlots => Set<BookingSlot>();

    /// <summary>
    /// Non-billable turnaround windows inserted automatically at booking confirmation
    /// when adjacent bookings require different simulator configurations.
    /// Rendered as grey-hatched on the availability calendar; not selectable by users.
    /// </summary>
    public DbSet<ReconfigurationSlot> ReconfigurationSlots => Set<ReconfigurationSlot>();

    /// <summary>
    /// Approval records for InternalStudent bookings. A CHECK constraint at the database
    /// level enforces that the reviewer cannot be the same user as the requester.
    /// </summary>
    public DbSet<BookingApproval> BookingApprovals => Set<BookingApproval>();

    /// <summary>Internal staff notes on a booking. Not visible to customers.</summary>
    public DbSet<BookingNote> BookingNotes => Set<BookingNote>();

    /// <summary>
    /// Immutable snapshots of discount rules applied at booking confirmation.
    /// No <c>updated_at</c> or <c>is_deleted</c> columns exist on this table —
    /// EF Core is configured to never update these rows after the initial insert.
    /// </summary>
    public DbSet<BookingDiscount> BookingDiscounts => Set<BookingDiscount>();

    // -------------------------------------------------------------------------
    // Invoicing & payments
    // -------------------------------------------------------------------------

    /// <summary>
    /// Billing documents raised against organisations. Only <c>Issued</c> and
    /// <c>Overdue</c> invoices contribute to <c>OrgAccount.current_balance_gbp</c>
    /// (maintained by a PostgreSQL trigger). Row-level security restricts visibility
    /// to the owning tenant.
    /// </summary>
    public DbSet<Invoice> Invoices => Set<Invoice>();

    /// <summary>
    /// Allocations distributing a payment across one or more invoices. The FK to
    /// <c>invoices</c> is added via a deferred <c>ALTER TABLE</c> in the DDL;
    /// this ordering is preserved in the EF migration.
    /// </summary>
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

    // -------------------------------------------------------------------------
    // Reference tables
    // -------------------------------------------------------------------------

    /// <summary>Customer classification tiers used to select pricing policies.</summary>
    public DbSet<CustomerClassRef> CustomerClasses => Set<CustomerClassRef>();

    /// <summary>Discount type definitions used in pricing rules and booking discount snapshots.</summary>
    public DbSet<DiscountTypeRef> DiscountTypes => Set<DiscountTypeRef>();

    /// <summary>Payment methods accepted for account payments.</summary>
    public DbSet<PaymentMethodRef> PaymentMethods => Set<PaymentMethodRef>();

    /// <summary>Lifecycle statuses for organisation accounts.</summary>
    public DbSet<AccountStatusRef> AccountStatuses => Set<AccountStatusRef>();

    // -------------------------------------------------------------------------
    // Reporting
    // -------------------------------------------------------------------------

    /// <summary>Named report definitions whose query and format are stored as <c>jsonb</c>.</summary>
    public DbSet<Report> Reports => Set<Report>();

    /// <summary>
    /// Asynchronous execution instances of a report. Processed by the reporting worker
    /// ECS task; output is written to the <c>fsbs-documents</c> S3 bucket and served
    /// only via pre-signed URLs.
    /// </summary>
    public DbSet<ReportRun> ReportRuns => Set<ReportRun>();

    // -------------------------------------------------------------------------
    // Model configuration
    // -------------------------------------------------------------------------

    /// <inheritdoc/>
    /// <remarks>
    /// Applies three layers of configuration in order:
    /// <list type="number">
    ///   <item><c>HasDefaultSchema("fsbs")</c> — all tables land in the <c>fsbs</c>
    ///     schema, never <c>public</c>.</item>
    ///   <item><c>ApplyConfigurationsFromAssembly</c> — discovers every
    ///     <c>IEntityTypeConfiguration&lt;T&gt;</c> in the
    ///     <c>FSBS.Infrastructure.Persistence.Entities</c> assembly. The anchor type
    ///     <see cref="BookingConfiguration"/> is used solely to identify that assembly;
    ///     it has no special significance beyond that.</item>
    ///   <item><see cref="ApplyGlobalQueryFilters"/> — registers per-request soft-delete
    ///     and tenant-isolation predicates.</item>
    /// </list>
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("fsbs");
        modelBuilder.HasPostgresEnum<TrainingType>("fsbs", "training_type");
        modelBuilder.HasPostgresEnum<InvitationStatus>("fsbs", "invitation_status");
        modelBuilder.HasPostgresEnum<InviteeRole>("fsbs", "invitee_role");
        modelBuilder.HasPostgresEnum<AvailabilityType>("fsbs", "availability_type");
        modelBuilder.HasPostgresEnum<BayStatus>("fsbs", "bay_status");
        modelBuilder.HasPostgresEnum<OrgRole>("fsbs", "org_role");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingConfiguration).Assembly);
        ApplyGlobalQueryFilters(modelBuilder);
    }

    /// <summary>
    /// Registers EF Core global query filters for soft-delete and multi-tenancy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Tenant filters</b> (<see cref="AppUser"/>, <see cref="Organisation"/>,
    /// <see cref="Course"/>): combine the soft-delete predicate with
    /// <c>e.TenantId == currentUser.TenantId</c>. The <c>currentUser</c> reference
    /// is captured from the constructor parameter — EF evaluates the lambda on
    /// each query execution, picking up the tenant ID from the current HTTP request's
    /// JWT rather than from a value fixed at context creation time.
    /// Staff users always carry the school's root tenant ID, so they see all data
    /// across every organisation without additional configuration.
    /// </para>
    /// <para>
    /// <b>Soft-delete only filters</b>: applied to the remaining
    /// <see cref="ISoftDeletable"/> entities that are not tenant-scoped. Logically
    /// deleted rows are hidden from all standard queries.
    /// </para>
    /// <para>
    /// <b>No filter</b>: entities that are neither soft-deletable nor tenant-scoped
    /// (<see cref="OrgAccount"/>, <see cref="AccountStatement"/>,
    /// <see cref="ReconfigurationSlot"/>, <see cref="BookingApproval"/>,
    /// <see cref="BookingDiscount"/>, <see cref="PaymentAllocation"/>,
    /// <see cref="ReportRun"/>, <see cref="ReconfigurationTemplate"/>) are omitted
    /// from this method intentionally — access to those rows is already constrained
    /// by the FK relationships traversed to reach them.
    /// </para>
    /// <para>
    /// To bypass all filters within a query (e.g. admin purge jobs, the nightly
    /// reconciliation Lambda), call <c>.IgnoreQueryFilters()</c> on the
    /// <c>IQueryable</c>.
    /// </para>
    /// </remarks>
    private void ApplyGlobalQueryFilters(ModelBuilder mb)
    {
        mb.Entity<AppUser>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == currentUser.TenantId);
        mb.Entity<Organisation>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == currentUser.TenantId);
        mb.Entity<Course>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == currentUser.TenantId);

        mb.Entity<Qualification>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<OrgMembership>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<AccountPayment>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<SimulatorUnit>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<AircraftType>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<SimulatorConfiguration>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<SimulatorBay>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<MaintenanceWindow>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<ScheduleTemplate>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<PricingPolicy>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<DiscountRule>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Module>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Lesson>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Enrolment>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<ProgressRecord>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Instructor>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<InstructorAvailability>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Booking>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<BookingSlot>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<BookingNote>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Invoice>().HasQueryFilter(e => !e.IsDeleted);
        mb.Entity<Report>().HasQueryFilter(e => !e.IsDeleted);
    }
}
