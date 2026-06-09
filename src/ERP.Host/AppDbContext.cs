using System.Linq.Expressions;
using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Admissions.Domain;
using ERP.Admissions.Infrastructure;
using ERP.Attendance.Domain;
using ERP.Attendance.Infrastructure;
using ERP.Auth.Domain;
using ERP.Auth.Infrastructure;
using ERP.Exams.Domain;
using ERP.Exams.Infrastructure;
using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Hostel.Domain;
using ERP.Hostel.Infrastructure;
using ERP.Transport.Domain;
using ERP.Transport.Infrastructure;
using TransportRoute = ERP.Transport.Domain.Route;
using ERP.Accreditation.Domain;
using ERP.Accreditation.Infrastructure;
using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.NIRF.Domain;
using ERP.NIRF.Infrastructure;
using ERP.ABC.Domain;
using ERP.ABC.Infrastructure;
using ERP.Compliance.Domain;
using ERP.Compliance.Infrastructure;
using ERP.Research.Domain;
using ERP.Research.Infrastructure;
using ERP.Alumni.Domain;
using ERP.Alumni.Infrastructure;
using ERP.Chatbot.Domain;
using ERP.Chatbot.Infrastructure;
using ERP.Analytics.Domain;
using ERP.Analytics.Infrastructure;
using ERP.MobileApi.Domain;
using ERP.MobileApi.Infrastructure;
using ERP.Reporting.Domain;
using ERP.Reporting.Infrastructure;
using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using ERP.Placement.Domain;
using ERP.Placement.Infrastructure;
using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.RBAC.Domain;
using ERP.RBAC.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Domain;
using ERP.SIS.Domain;
using ERP.SIS.Infrastructure;
using ERP.Tenants.Domain;
using ERP.Tenants.Infrastructure;
using ERP.Timetable.Domain;
using ERP.Timetable.Infrastructure;
using ERP.Users.Domain;
using ERP.Users.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Host;

public class AppDbContext : DbContext,
    ERP.Tenants.Application.Commands.ITenantsDbContext,
    ERP.Tenants.Application.Queries.ITenantsReadDbContext,
    ERP.Auth.Application.Commands.IAuthDbContext,
    IRbacDbContext,
    IUsersDbContext,
    IAdmissionsDbContext,
    ISisDbContext,
    IAcademicsDbContext,
    ITimetableDbContext,
    IAttendanceDbContext,
    IExamsDbContext,
    ILmsDbContext,
    IFeesDbContext,
    IFinanceDbContext,
    IHrmsDbContext,
    ILibraryDbContext,
    IHostelDbContext,
    ITransportDbContext,
    IPlacementDbContext,
    IAccreditationDbContext,
    INaacDbContext,
    IObeDbContext,
    INirfDbContext,
    IComplianceDbContext,
    IAbcDbContext,
    IResearchDbContext,
    IAlumniDbContext,
    IChatbotDbContext,
    IAnalyticsDbContext,
    IMobileDbContext,
    IReportingDbContext
{
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant, ICurrentUser? currentUser = null)
        : base(options)
    {
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    // Tenants
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();

    // Auth
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // RBAC
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    // Users
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    // Admissions
    public DbSet<AdmissionApplication> Applications => Set<AdmissionApplication>();
    public DbSet<ApplicationDocument> ApplicationDocuments => Set<ApplicationDocument>();
    public DbSet<WorkflowAuditEntry> WorkflowAuditEntries => Set<WorkflowAuditEntry>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<SeatMatrix> SeatMatrices => Set<SeatMatrix>();

    // SIS
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
    public DbSet<StudentFamily> StudentFamilyDetails => Set<StudentFamily>();

    // Academics
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AcademicProgram> AcademicPrograms => Set<AcademicProgram>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<CurriculumEntry> CurriculumEntries => Set<CurriculumEntry>();
    public DbSet<CourseOutcome> CourseOutcomes => Set<CourseOutcome>();
    public DbSet<ProgramOutcome> ProgramOutcomes => Set<ProgramOutcome>();
    public DbSet<ProgramSpecificOutcome> ProgramSpecificOutcomes => Set<ProgramSpecificOutcome>();

    // Attendance
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<RegularizationRequest> RegularizationRequests => Set<RegularizationRequest>();
    public DbSet<BiometricLog> BiometricLogs => Set<BiometricLog>();

    // Exams
    public DbSet<ExamSchedule> ExamSchedules => Set<ExamSchedule>();
    public DbSet<SeatAllocation> SeatAllocations => Set<SeatAllocation>();
    public DbSet<GradingScheme> GradingSchemes => Set<GradingScheme>();
    public DbSet<GradeRule> GradeRules => Set<GradeRule>();
    public DbSet<InternalMark> InternalMarks => Set<InternalMark>();
    public DbSet<ExternalMark> ExternalMarks => Set<ExternalMark>();
    public DbSet<StudentResult> StudentResults => Set<StudentResult>();
    public DbSet<ArrearRegistration> ArrearRegistrations => Set<ArrearRegistration>();

    // Fees
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<FeeComponent> FeeComponents => Set<FeeComponent>();
    public DbSet<InstallmentSchedule> InstallmentSchedules => Set<InstallmentSchedule>();
    public DbSet<StudentFeeAccount> StudentFeeAccounts => Set<StudentFeeAccount>();
    public DbSet<FeeInstallment> FeeInstallments => Set<FeeInstallment>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<StudentScholarship> StudentScholarships => Set<StudentScholarship>();
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();

    // Finance
    public DbSet<Account> GlAccounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<BudgetLine> BudgetLines => Set<BudgetLine>();
    public DbSet<BankStatement> BankStatements => Set<BankStatement>();
    public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();

    // HRMS
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<RecruitmentRequisition> RecruitmentRequisitions => Set<RecruitmentRequisition>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveApplication> LeaveApplications => Set<LeaveApplication>();
    public DbSet<SalaryStructure> SalaryStructures => Set<SalaryStructure>();
    public DbSet<SalaryComponent> SalaryComponents => Set<SalaryComponent>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollEntry> PayrollEntries => Set<PayrollEntry>();
    public DbSet<Appraisal> Appraisals => Set<Appraisal>();

    // Hostel
    public DbSet<HostelBlock> HostelBlocks => Set<HostelBlock>();
    public DbSet<HostelRoom> HostelRooms => Set<HostelRoom>();
    public DbSet<RoomAllocation> RoomAllocations => Set<RoomAllocation>();
    public DbSet<WaitlistEntry> HostelWaitlist => Set<WaitlistEntry>();
    public DbSet<VisitorEntry> VisitorEntries => Set<VisitorEntry>();

    // Accreditation
    public DbSet<EvidenceTag> EvidenceTags => Set<EvidenceTag>();
    public DbSet<EvidenceSummary> EvidenceSummaries => Set<EvidenceSummary>();

    // NIRF
    public DbSet<NirfSubmission> NirfSubmissions => Set<NirfSubmission>();
    public DbSet<NirfParameterScore> NirfParameterScores => Set<NirfParameterScore>();
    public DbSet<NirfRankEntry> NirfRankHistory => Set<NirfRankEntry>();

    // Chatbot
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    // MobileApi
    public DbSet<DeviceRegistration> DeviceRegistrations => Set<DeviceRegistration>();
    public DbSet<PushNotification> PushNotifications => Set<PushNotification>();

    // Reporting
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<ReportColumn> ReportColumns => Set<ReportColumn>();
    public DbSet<ReportFilter> ReportFilters => Set<ReportFilter>();
    public DbSet<ReportSchedule> ReportSchedules => Set<ReportSchedule>();
    public DbSet<ReportExecution> ReportExecutions => Set<ReportExecution>();

    // Analytics
    public DbSet<StudentRiskScore> StudentRiskScores => Set<StudentRiskScore>();
    public DbSet<FeeDefaultRiskScore> FeeDefaultRiskScores => Set<FeeDefaultRiskScore>();
    public DbSet<PlacementScore> PlacementScores => Set<PlacementScore>();

    // Alumni
    public DbSet<AlumniProfile> AlumniProfiles => Set<AlumniProfile>();
    public DbSet<AlumniEvent> AlumniEvents => Set<AlumniEvent>();
    public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
    public DbSet<DonationCampaign> DonationCampaigns => Set<DonationCampaign>();
    public DbSet<DonationPledge> DonationPledges => Set<DonationPledge>();

    // Research
    public DbSet<ResearchProject> ResearchProjects => Set<ResearchProject>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Publication> Publications => Set<Publication>();
    public DbSet<Patent> Patents => Set<Patent>();
    public DbSet<Grant> Grants => Set<Grant>();
    public DbSet<GrantDisbursement> GrantDisbursements => Set<GrantDisbursement>();

    // ABC
    public DbSet<StudentAbcProfile> StudentAbcProfiles => Set<StudentAbcProfile>();
    public DbSet<CreditTransfer> CreditTransfers => Set<CreditTransfer>();
    public DbSet<AcademicPathway> AcademicPathways => Set<AcademicPathway>();

    // Compliance
    public DbSet<ComplianceItem> ComplianceItems => Set<ComplianceItem>();
    public DbSet<AisheReturn> AisheReturns => Set<AisheReturn>();
    public DbSet<ComplianceNotification> ComplianceNotifications => Set<ComplianceNotification>();

    // OBE
    public DbSet<CoPoMapping> CoPoMappings => Set<CoPoMapping>();
    public DbSet<CoPsoMapping> CoPsoMappings => Set<CoPsoMapping>();
    public DbSet<DirectAttainment> DirectAttainments => Set<DirectAttainment>();
    public DbSet<IndirectAttainmentSurvey> IndirectSurveys => Set<IndirectAttainmentSurvey>();
    public DbSet<SurveyQuestion> SurveyQuestions => Set<SurveyQuestion>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<AttainmentGap> AttainmentGaps => Set<AttainmentGap>();
    public DbSet<ActionPlan> ActionPlans => Set<ActionPlan>();

    // NAAC
    public DbSet<SsrReport> SsrReports => Set<SsrReport>();
    public DbSet<SsrSection> SsrSections => Set<SsrSection>();
    public DbSet<DvvQuery> DvvQueries => Set<DvvQuery>();
    public DbSet<AqarReport> AqarReports => Set<AqarReport>();
    public DbSet<AqarSection> AqarSections => Set<AqarSection>();

    // Placement
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<PlacementDrive> Drives => Set<PlacementDrive>();
    public DbSet<DriveRegistration> Registrations => Set<DriveRegistration>();
    public DbSet<PlacementOffer> Offers => Set<PlacementOffer>();

    // Transport
    public DbSet<TransportRoute> Routes => Set<TransportRoute>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<RouteAssignment> RouteAssignments => Set<RouteAssignment>();
    public DbSet<GpsLocation> GpsLocations => Set<GpsLocation>();

    // Library
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<BookIssue> BookIssues => Set<BookIssue>();
    public DbSet<Fine> LibraryFines => Set<Fine>();
    public DbSet<LibraryPolicy> LibraryPolicies => Set<LibraryPolicy>();

    // LMS
    public DbSet<CourseContent> CourseContents => Set<CourseContent>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();
    public DbSet<ForumThread> ForumThreads => Set<ForumThread>();
    public DbSet<ForumReply> ForumReplies => Set<ForumReply>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<StudentProgress> StudentProgresses => Set<StudentProgress>();

    // Timetable
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();
    public DbSet<FacultyWorkload> FacultyWorkloads => Set<FacultyWorkload>();
    public DbSet<FacultySubjectAssignment> FacultySubjectAssignments => Set<FacultySubjectAssignment>();
    public DbSet<SubstituteAssignment> SubstituteAssignments => Set<SubstituteAssignment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all entity configurations from this assembly and module assemblies
        builder.ApplyConfiguration(new TenantConfiguration());
        builder.ApplyConfiguration(new TenantSettingsConfiguration());
        builder.ApplyConfiguration(new UserConfiguration());
        builder.ApplyConfiguration(new RefreshTokenConfiguration());
        builder.ApplyConfiguration(new RoleConfiguration());
        builder.ApplyConfiguration(new PermissionConfiguration());
        builder.ApplyConfiguration(new RolePermissionConfiguration());
        builder.ApplyConfiguration(new UserRoleConfiguration());
        builder.ApplyConfiguration(new MenuItemConfiguration());
        builder.ApplyConfiguration(new UserProfileConfiguration());
        builder.ApplyConfiguration(new AdmissionApplicationConfiguration());
        builder.ApplyConfiguration(new ApplicationDocumentConfiguration());
        builder.ApplyConfiguration(new WorkflowAuditEntryConfiguration());
        builder.ApplyConfiguration(new WorkflowDefinitionConfiguration());
        builder.ApplyConfiguration(new SeatMatrixConfiguration());
        builder.ApplyConfiguration(new StudentConfiguration());
        builder.ApplyConfiguration(new StudentDocumentConfiguration());
        builder.ApplyConfiguration(new StudentFamilyConfiguration());
        builder.ApplyConfiguration(new DepartmentConfiguration());
        builder.ApplyConfiguration(new AcademicProgramConfiguration());
        builder.ApplyConfiguration(new CourseConfiguration());
        builder.ApplyConfiguration(new SubjectConfiguration());
        builder.ApplyConfiguration(new AcademicYearConfiguration());
        builder.ApplyConfiguration(new SemesterConfiguration());
        builder.ApplyConfiguration(new BatchConfiguration());
        builder.ApplyConfiguration(new CurriculumEntryConfiguration());
        builder.ApplyConfiguration(new CourseOutcomeConfiguration());
        builder.ApplyConfiguration(new ProgramOutcomeConfiguration());
        builder.ApplyConfiguration(new ProgramSpecificOutcomeConfiguration());
        builder.ApplyConfiguration(new RoomConfiguration());
        builder.ApplyConfiguration(new TimeSlotConfiguration());
        builder.ApplyConfiguration(new TimetableEntryConfiguration());
        builder.ApplyConfiguration(new FacultyWorkloadConfiguration());
        builder.ApplyConfiguration(new FacultySubjectAssignmentConfiguration());
        builder.ApplyConfiguration(new SubstituteAssignmentConfiguration());
        builder.ApplyConfiguration(new AttendanceSessionConfiguration());
        builder.ApplyConfiguration(new AttendanceRecordConfiguration());
        builder.ApplyConfiguration(new RegularizationRequestConfiguration());
        builder.ApplyConfiguration(new BiometricLogConfiguration());
        builder.ApplyConfiguration(new ExamScheduleConfiguration());
        builder.ApplyConfiguration(new SeatAllocationConfiguration());
        builder.ApplyConfiguration(new GradingSchemeConfiguration());
        builder.ApplyConfiguration(new GradeRuleConfiguration());
        builder.ApplyConfiguration(new InternalMarkConfiguration());
        builder.ApplyConfiguration(new ExternalMarkConfiguration());
        builder.ApplyConfiguration(new StudentResultConfiguration());
        builder.ApplyConfiguration(new ArrearRegistrationConfiguration());
        builder.ApplyConfiguration(new CourseContentConfiguration());
        builder.ApplyConfiguration(new AssignmentConfiguration());
        builder.ApplyConfiguration(new AssignmentSubmissionConfiguration());
        builder.ApplyConfiguration(new QuizConfiguration());
        builder.ApplyConfiguration(new QuizQuestionConfiguration());
        builder.ApplyConfiguration(new QuizAttemptConfiguration());
        builder.ApplyConfiguration(new QuizAnswerConfiguration());
        builder.ApplyConfiguration(new ForumThreadConfiguration());
        builder.ApplyConfiguration(new ForumReplyConfiguration());
        builder.ApplyConfiguration(new AnnouncementConfiguration());
        builder.ApplyConfiguration(new StudentProgressConfiguration());
        builder.ApplyConfiguration(new FeeStructureConfiguration());
        builder.ApplyConfiguration(new FeeComponentConfiguration());
        builder.ApplyConfiguration(new InstallmentScheduleConfiguration());
        builder.ApplyConfiguration(new StudentFeeAccountConfiguration());
        builder.ApplyConfiguration(new FeeInstallmentConfiguration());
        builder.ApplyConfiguration(new FeePaymentConfiguration());
        builder.ApplyConfiguration(new ScholarshipConfiguration());
        builder.ApplyConfiguration(new StudentScholarshipConfiguration());
        builder.ApplyConfiguration(new RefundRequestConfiguration());
        builder.ApplyConfiguration(new AccountConfiguration());
        builder.ApplyConfiguration(new JournalEntryConfiguration());
        builder.ApplyConfiguration(new JournalLineConfiguration());
        builder.ApplyConfiguration(new BudgetConfiguration());
        builder.ApplyConfiguration(new BudgetLineConfiguration());
        builder.ApplyConfiguration(new BankStatementConfiguration());
        builder.ApplyConfiguration(new BankStatementLineConfiguration());
        builder.ApplyConfiguration(new EmployeeConfiguration());
        builder.ApplyConfiguration(new EmployeeDocumentConfiguration());
        builder.ApplyConfiguration(new RecruitmentRequisitionConfiguration());
        builder.ApplyConfiguration(new JobApplicationConfiguration());
        builder.ApplyConfiguration(new LeaveTypeConfiguration());
        builder.ApplyConfiguration(new LeaveBalanceConfiguration());
        builder.ApplyConfiguration(new LeaveApplicationConfiguration());
        builder.ApplyConfiguration(new SalaryStructureConfiguration());
        builder.ApplyConfiguration(new SalaryComponentConfiguration());
        builder.ApplyConfiguration(new PayrollRunConfiguration());
        builder.ApplyConfiguration(new PayrollEntryConfiguration());
        builder.ApplyConfiguration(new AppraisalConfiguration());
        builder.ApplyConfiguration(new HostelBlockConfiguration());
        builder.ApplyConfiguration(new HostelRoomConfiguration());
        builder.ApplyConfiguration(new RoomAllocationConfiguration());
        builder.ApplyConfiguration(new WaitlistEntryConfiguration());
        builder.ApplyConfiguration(new VisitorEntryConfiguration());
        builder.ApplyConfiguration(new BookConfiguration());
        builder.ApplyConfiguration(new BookCopyConfiguration());
        builder.ApplyConfiguration(new BookIssueConfiguration());
        builder.ApplyConfiguration(new FineConfiguration());
        builder.ApplyConfiguration(new LibraryPolicyConfiguration());
        builder.ApplyConfiguration(new RouteConfiguration());
        builder.ApplyConfiguration(new RouteStopConfiguration());
        builder.ApplyConfiguration(new VehicleConfiguration());
        builder.ApplyConfiguration(new DriverConfiguration());
        builder.ApplyConfiguration(new RouteAssignmentConfiguration());
        builder.ApplyConfiguration(new GpsLocationConfiguration());
        builder.ApplyConfiguration(new CompanyConfiguration());
        builder.ApplyConfiguration(new PlacementDriveConfiguration());
        builder.ApplyConfiguration(new DriveRegistrationConfiguration());
        builder.ApplyConfiguration(new PlacementOfferConfiguration());
        builder.ApplyConfiguration(new EvidenceTagConfiguration());
        builder.ApplyConfiguration(new EvidenceSummaryConfiguration());
        builder.ApplyConfiguration(new SsrReportConfiguration());
        builder.ApplyConfiguration(new SsrSectionConfiguration());
        builder.ApplyConfiguration(new DvvQueryConfiguration());
        builder.ApplyConfiguration(new AqarReportConfiguration());
        builder.ApplyConfiguration(new AqarSectionConfiguration());
        builder.ApplyConfiguration(new NirfSubmissionConfiguration());
        builder.ApplyConfiguration(new NirfParameterScoreConfiguration());
        builder.ApplyConfiguration(new NirfRankEntryConfiguration());
        builder.ApplyConfiguration(new ResearchProjectConfiguration());
        builder.ApplyConfiguration(new ProjectMemberConfiguration());
        builder.ApplyConfiguration(new PublicationConfiguration());
        builder.ApplyConfiguration(new PatentConfiguration());
        builder.ApplyConfiguration(new GrantConfiguration());
        builder.ApplyConfiguration(new GrantDisbursementConfiguration());
        builder.ApplyConfiguration(new ChatSessionConfiguration());
        builder.ApplyConfiguration(new ChatMessageConfiguration());
        builder.ApplyConfiguration(new DeviceRegistrationConfiguration());
        builder.ApplyConfiguration(new PushNotificationConfiguration());
        builder.ApplyConfiguration(new ReportDefinitionConfiguration());
        builder.ApplyConfiguration(new ReportColumnConfiguration());
        builder.ApplyConfiguration(new ReportFilterConfiguration());
        builder.ApplyConfiguration(new ReportScheduleConfiguration());
        builder.ApplyConfiguration(new ReportExecutionConfiguration());
        builder.ApplyConfiguration(new StudentRiskScoreConfiguration());
        builder.ApplyConfiguration(new FeeDefaultRiskScoreConfiguration());
        builder.ApplyConfiguration(new PlacementScoreConfiguration());
        builder.ApplyConfiguration(new AlumniProfileConfiguration());
        builder.ApplyConfiguration(new AlumniEventConfiguration());
        builder.ApplyConfiguration(new EventRegistrationConfiguration());
        builder.ApplyConfiguration(new DonationCampaignConfiguration());
        builder.ApplyConfiguration(new DonationPledgeConfiguration());
        builder.ApplyConfiguration(new StudentAbcProfileConfiguration());
        builder.ApplyConfiguration(new CreditTransferConfiguration());
        builder.ApplyConfiguration(new AcademicPathwayConfiguration());
        builder.ApplyConfiguration(new ComplianceItemConfiguration());
        builder.ApplyConfiguration(new AisheReturnConfiguration());
        builder.ApplyConfiguration(new ComplianceNotificationConfiguration());
        builder.ApplyConfiguration(new CoPoMappingConfiguration());
        builder.ApplyConfiguration(new CoPsoMappingConfiguration());
        builder.ApplyConfiguration(new DirectAttainmentConfiguration());
        builder.ApplyConfiguration(new IndirectAttainmentSurveyConfiguration());
        builder.ApplyConfiguration(new SurveyQuestionConfiguration());
        builder.ApplyConfiguration(new SurveyResponseConfiguration());
        builder.ApplyConfiguration(new AttainmentGapConfiguration());
        builder.ApplyConfiguration(new ActionPlanConfiguration());

        // Global query filters for TenantEntity
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var tenantIdProperty = Expression.Property(parameter, nameof(TenantEntity.TenantId));

                // We use a closure that reads from _currentTenant at query time
                var currentTenantRef = Expression.Constant(_currentTenant);
                var tenantIdValue = Expression.Property(currentTenantRef, nameof(ICurrentTenant.TenantId));

                // TenantId == _currentTenant.TenantId ?? Guid.Empty
                var coalesce = Expression.Coalesce(tenantIdValue,
                    Expression.Constant(Guid.Empty));

                var equals = Expression.Equal(tenantIdProperty, coalesce);
                var lambda = Expression.Lambda(equals, parameter);

                builder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var userId = _currentUser?.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                if (userId.HasValue && entry.Entity.CreatedBy is null)
                    entry.Entity.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
