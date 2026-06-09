using ERP.Reporting.Domain;

namespace ERP.Reporting.Application.Services;

public static class PredefinedReportRegistry
{
    public static IReadOnlyList<PredefinedReport> GetAll() => _reports;

    private static readonly IReadOnlyList<PredefinedReport> _reports = new[]
    {
        new PredefinedReport("RPT-ACAD-001", "Student Enrollment Report", ReportCategory.Academic,
            @"SELECT s.StudentNumber, CONCAT(s.FirstName,' ',s.LastName) AS StudentName,
                     s.ProgramName, s.Category, s.AcademicYear, s.Semester, s.IsActive
              FROM students s
              WHERE s.TenantId = @TenantId AND s.IsDeleted = 0
                AND (@AcademicYear IS NULL OR s.AcademicYear = @AcademicYear)
              ORDER BY s.ProgramName, s.StudentNumber",
            new[] { "StudentNumber","StudentName","ProgramName","Category","AcademicYear","Semester","IsActive" },
            new[] { new FilterDef("AcademicYear","Academic Year","int",false,null) }),

        new PredefinedReport("RPT-ACAD-002", "Program-wise Enrollment Summary", ReportCategory.Academic,
            @"SELECT ProgramName, COUNT(*) AS TotalStudents,
                     SUM(CASE WHEN IsActive=1 THEN 1 ELSE 0 END) AS ActiveStudents
              FROM students WHERE TenantId = @TenantId AND IsDeleted = 0
                AND (@AcademicYear IS NULL OR AcademicYear = @AcademicYear)
              GROUP BY ProgramName ORDER BY TotalStudents DESC",
            new[] { "ProgramName","TotalStudents","ActiveStudents" },
            new[] { new FilterDef("AcademicYear","Academic Year","int",false,null) }),

        new PredefinedReport("RPT-ATT-001", "Student Attendance Summary", ReportCategory.Attendance,
            @"SELECT CONCAT(s.FirstName,' ',s.LastName) AS StudentName, s.ProgramName,
                     COUNT(ar.Id) AS TotalClasses,
                     SUM(CASE WHEN ar.Status=1 THEN 1 ELSE 0 END) AS Present,
                     ROUND(SUM(CASE WHEN ar.Status=1 THEN 1 ELSE 0 END)*100.0/NULLIF(COUNT(ar.Id),0),2) AS AttendancePercent
              FROM students s
              LEFT JOIN attendance_records ar ON ar.StudentId=s.Id AND ar.IsDeleted=0
              LEFT JOIN attendance_sessions asn ON asn.Id=ar.SessionId AND asn.IsDeleted=0
              WHERE s.TenantId=@TenantId AND s.IsDeleted=0
                AND (@SemesterId IS NULL OR asn.SemesterId=@SemesterId)
              GROUP BY s.Id, s.FirstName, s.LastName, s.ProgramName
              ORDER BY AttendancePercent",
            new[] { "StudentName","ProgramName","TotalClasses","Present","AttendancePercent" },
            new[] { new FilterDef("SemesterId","Semester","guid",false,null) }),

        new PredefinedReport("RPT-ATT-002", "Below 75% Attendance Alert", ReportCategory.Attendance,
            @"SELECT CONCAT(s.FirstName,' ',s.LastName) AS StudentName, s.ProgramName,
                     ROUND(SUM(CASE WHEN ar.Status=1 THEN 1 ELSE 0 END)*100.0/NULLIF(COUNT(ar.Id),0),2) AS AttendancePercent
              FROM students s
              JOIN attendance_records ar ON ar.StudentId=s.Id AND ar.IsDeleted=0
              JOIN attendance_sessions asn ON asn.Id=ar.SessionId AND asn.IsDeleted=0
              WHERE s.TenantId=@TenantId AND s.IsDeleted=0
                AND (@SemesterId IS NULL OR asn.SemesterId=@SemesterId)
              GROUP BY s.Id, s.FirstName, s.LastName, s.ProgramName
              HAVING AttendancePercent < 75
              ORDER BY AttendancePercent",
            new[] { "StudentName","ProgramName","AttendancePercent" },
            new[] { new FilterDef("SemesterId","Semester","guid",true,null) }),

        new PredefinedReport("RPT-EXAM-001", "Subject-wise Pass/Fail Report", ReportCategory.Examinations,
            @"SELECT sr.SubjectName,
                     COUNT(*) AS TotalStudents,
                     SUM(CASE WHEN sr.Status=1 THEN 1 ELSE 0 END) AS Passed,
                     SUM(CASE WHEN sr.Status=2 THEN 1 ELSE 0 END) AS Failed,
                     ROUND(SUM(CASE WHEN sr.Status=1 THEN 1 ELSE 0 END)*100.0/NULLIF(COUNT(*),0),2) AS PassPercent
              FROM student_results sr
              WHERE sr.TenantId=@TenantId AND sr.IsDeleted=0 AND sr.IsPublished=1
                AND (@SemesterId IS NULL OR sr.SemesterId=@SemesterId)
              GROUP BY sr.SubjectName ORDER BY PassPercent",
            new[] { "SubjectName","TotalStudents","Passed","Failed","PassPercent" },
            new[] { new FilterDef("SemesterId","Semester","guid",false,null) }),

        new PredefinedReport("RPT-EXAM-002", "Student Result Card", ReportCategory.Examinations,
            @"SELECT sr.SubjectName, sr.InternalMarks, sr.ExternalMarks, sr.TotalMarks,
                     sr.GradeLetter, sr.GradePoints, sr.Status
              FROM student_results sr
              WHERE sr.TenantId=@TenantId AND sr.StudentId=@StudentId
                AND sr.SemesterId=@SemesterId AND sr.IsPublished=1 AND sr.IsDeleted=0
              ORDER BY sr.SubjectName",
            new[] { "SubjectName","InternalMarks","ExternalMarks","TotalMarks","GradeLetter","GradePoints","Status" },
            new[] {
                new FilterDef("StudentId","Student","guid",true,null),
                new FilterDef("SemesterId","Semester","guid",true,null)
            }),

        new PredefinedReport("RPT-FIN-001", "Fee Collection Summary", ReportCategory.Finance,
            @"SELECT CONCAT(s.FirstName,' ',s.LastName) AS StudentName,
                     sfa.TotalAmount, sfa.DiscountAmount, sfa.NetAmount, sfa.PaidAmount, sfa.DueAmount,
                     sfa.IsFullyPaid, sfa.AcademicYear
              FROM student_fee_accounts sfa
              JOIN students s ON s.Id=sfa.StudentId AND s.IsDeleted=0
              WHERE sfa.TenantId=@TenantId AND sfa.IsDeleted=0
                AND (@AcademicYear IS NULL OR sfa.AcademicYear=@AcademicYear)
              ORDER BY sfa.DueAmount DESC",
            new[] { "StudentName","TotalAmount","DiscountAmount","NetAmount","PaidAmount","DueAmount","IsFullyPaid" },
            new[] { new FilterDef("AcademicYear","Academic Year","int",false,null) }),

        new PredefinedReport("RPT-FIN-002", "Fee Defaulters List", ReportCategory.Finance,
            @"SELECT CONCAT(s.FirstName,' ',s.LastName) AS StudentName, s.ProgramName,
                     sfa.DueAmount, sfa.AcademicYear
              FROM student_fee_accounts sfa
              JOIN students s ON s.Id=sfa.StudentId AND s.IsDeleted=0
              WHERE sfa.TenantId=@TenantId AND sfa.DueAmount>0 AND sfa.IsDeleted=0
                AND (@AcademicYear IS NULL OR sfa.AcademicYear=@AcademicYear)
              ORDER BY sfa.DueAmount DESC",
            new[] { "StudentName","ProgramName","DueAmount","AcademicYear" },
            new[] { new FilterDef("AcademicYear","Academic Year","int",false,null) }),

        new PredefinedReport("RPT-HR-001", "Employee List", ReportCategory.HR,
            @"SELECT EmployeeCode, CONCAT(FirstName,' ',LastName) AS Name, Designation,
                     EmploymentType, Status, JoiningDate
              FROM employees
              WHERE TenantId=@TenantId AND IsDeleted=0
              ORDER BY EmployeeCode",
            new[] { "EmployeeCode","Name","Designation","EmploymentType","Status","JoiningDate" },
            Array.Empty<FilterDef>()),

        new PredefinedReport("RPT-HR-002", "Payroll Summary", ReportCategory.HR,
            @"SELECT CONCAT(e.FirstName,' ',e.LastName) AS EmployeeName,
                     pe.GrossPay, pe.PfEmployee, pe.TdsAmount, pe.TotalDeductions, pe.NetPay
              FROM payroll_entries pe
              JOIN employees e ON e.Id=pe.EmployeeId AND e.IsDeleted=0
              JOIN payroll_runs pr ON pr.Id=pe.PayrollRunId AND pr.IsDeleted=0
              WHERE pe.TenantId=@TenantId AND pe.IsDeleted=0
                AND (@Month IS NULL OR pr.Month=@Month)
                AND (@Year IS NULL OR pr.Year=@Year)
              ORDER BY EmployeeName",
            new[] { "EmployeeName","GrossPay","PfEmployee","TdsAmount","TotalDeductions","NetPay" },
            new[] {
                new FilterDef("Month","Month","int",false,null),
                new FilterDef("Year","Year","int",false,null)
            }),

        new PredefinedReport("RPT-LIB-001", "Overdue Books Report", ReportCategory.Library,
            @"SELECT bi.MemberName, b.Title, b.Authors, bi.IssuedAt, bi.DueDate,
                     DATEDIFF(CURDATE(), bi.DueDate) AS DaysOverdue
              FROM book_issues bi
              JOIN book_copies bc ON bc.Id=bi.CopyId AND bc.IsDeleted=0
              JOIN library_books b ON b.Id=bc.BookId AND b.IsDeleted=0
              WHERE bi.TenantId=@TenantId AND bi.Status IN (1,3) AND bi.IsDeleted=0
                AND bi.DueDate < CURDATE()
              ORDER BY DaysOverdue DESC",
            new[] { "MemberName","Title","Authors","IssuedAt","DueDate","DaysOverdue" },
            Array.Empty<FilterDef>()),

        new PredefinedReport("RPT-PLACE-001", "Placement Statistics", ReportCategory.Placement,
            @"SELECT pd.CompanyName, pd.JobRole, pd.PackageLpa,
                     COUNT(po.Id) AS TotalOffers, MAX(po.OfferedPackageLpa) AS HighestPackage
              FROM placement_offers po
              JOIN placement_drives pd ON pd.Id=po.DriveId AND pd.IsDeleted=0
              WHERE po.TenantId=@TenantId AND po.IsDeleted=0
                AND (@AcademicYear IS NULL OR pd.AcademicYear=@AcademicYear)
              GROUP BY pd.CompanyName, pd.JobRole, pd.PackageLpa
              ORDER BY TotalOffers DESC",
            new[] { "CompanyName","JobRole","PackageLpa","TotalOffers","HighestPackage" },
            new[] { new FilterDef("AcademicYear","Academic Year","int",false,null) }),

        new PredefinedReport("RPT-ADM-001", "Admission Applications Status", ReportCategory.Admissions,
            @"SELECT ApplicantName, ApplicantEmail, ProgramName, Category, State, AcademicYear, CreatedAt
              FROM admission_applications
              WHERE TenantId=@TenantId AND IsDeleted=0
                AND (@AcademicYear IS NULL OR AcademicYear=@AcademicYear)
              ORDER BY CreatedAt DESC",
            new[] { "ApplicantName","ApplicantEmail","ProgramName","Category","State","AcademicYear" },
            new[] { new FilterDef("AcademicYear","Academic Year","int",false,null) }),

        new PredefinedReport("RPT-HOST-001", "Hostel Occupancy Report", ReportCategory.Hostel,
            @"SELECT b.Name AS Block, r.RoomNumber, r.RoomType, r.Capacity, r.OccupiedCount,
                     (r.Capacity - r.OccupiedCount) AS Available, r.Status
              FROM hostel_rooms r
              JOIN hostel_blocks b ON b.Id=r.BlockId AND b.IsDeleted=0
              WHERE r.TenantId=@TenantId AND r.IsActive=1 AND r.IsDeleted=0
              ORDER BY b.Name, r.RoomNumber",
            new[] { "Block","RoomNumber","RoomType","Capacity","OccupiedCount","Available","Status" },
            Array.Empty<FilterDef>()),

        new PredefinedReport("RPT-ANLX-001", "At-Risk Students", ReportCategory.Academic,
            @"SELECT StudentName, ProgramName, AttendancePercent, AverageMarksPercent, RiskScore, RiskLevel, ComputedAt
              FROM student_risk_scores
              WHERE TenantId=@TenantId AND IsDeleted=0
                AND (@AcademicYear IS NULL OR AcademicYear=@AcademicYear)
                AND (@MinRiskLevel IS NULL OR RiskLevel >= @MinRiskLevel)
              ORDER BY RiskScore DESC",
            new[] { "StudentName","ProgramName","AttendancePercent","AverageMarksPercent","RiskScore","RiskLevel" },
            new[] {
                new FilterDef("AcademicYear","Academic Year","int",false,null),
                new FilterDef("MinRiskLevel","Min Risk Level","int",false,null)
            }),

        new PredefinedReport("RPT-RES-001", "Faculty Publications", ReportCategory.Accreditation,
            @"SELECT FacultyName, Title, PublicationType, VenueName, PublicationYear, ImpactFactor, `Index`
              FROM publications
              WHERE TenantId=@TenantId AND IsDeleted=0
                AND (@Year IS NULL OR PublicationYear=@Year)
              ORDER BY PublicationYear DESC, ImpactFactor DESC",
            new[] { "FacultyName","Title","PublicationType","VenueName","PublicationYear","ImpactFactor","Index" },
            new[] { new FilterDef("Year","Year","int",false,null) }),

        new PredefinedReport("RPT-ALUM-001", "Alumni Directory", ReportCategory.Custom,
            @"SELECT CONCAT(FirstName,' ',LastName) AS Name, GraduationYear, ProgramName,
                     CurrentEmployer, CurrentJobTitle, CurrentCity, CurrentCountry
              FROM alumni_profiles
              WHERE TenantId=@TenantId AND IsDirectoryVisible=1 AND IsDeleted=0
                AND (@GraduationYear IS NULL OR GraduationYear=@GraduationYear)
              ORDER BY GraduationYear DESC, Name",
            new[] { "Name","GraduationYear","ProgramName","CurrentEmployer","CurrentJobTitle","CurrentCity" },
            new[] { new FilterDef("GraduationYear","Graduation Year","int",false,null) }),

        new PredefinedReport("RPT-NIRF-001", "NIRF Score History", ReportCategory.Accreditation,
            @"SELECT ns.RankingYear, ns.Category, ns.OverallScore, ns.EstimatedRank,
                     GROUP_CONCAT(CONCAT(nps.Parameter,':',nps.RawScore) SEPARATOR ', ') AS ParameterScores
              FROM nirf_submissions ns
              LEFT JOIN nirf_parameter_scores nps ON nps.SubmissionId=ns.Id AND nps.IsDeleted=0
              WHERE ns.TenantId=@TenantId AND ns.IsDeleted=0
              GROUP BY ns.Id ORDER BY ns.RankingYear DESC",
            new[] { "RankingYear","Category","OverallScore","EstimatedRank","ParameterScores" },
            Array.Empty<FilterDef>()),

        new PredefinedReport("RPT-COMP-001", "Compliance Calendar", ReportCategory.Accreditation,
            @"SELECT Title, Authority, DueDate, ResponsiblePersonName, Status, AcademicYear
              FROM compliance_items
              WHERE TenantId=@TenantId AND IsDeleted=0
                AND (@AcademicYear IS NULL OR AcademicYear=@AcademicYear)
              ORDER BY DueDate",
            new[] { "Title","Authority","DueDate","ResponsiblePersonName","Status" },
            new[] { new FilterDef("AcademicYear","Academic Year","int",false,null) }),

        new PredefinedReport("RPT-TRANS-001", "Route Passenger List", ReportCategory.Custom,
            @"SELECT tr.Name AS RouteName, ra.MemberName, ra.MemberType, rs.Name AS StopName, rs.Sequence
              FROM route_assignments ra
              JOIN transport_routes tr ON tr.Id=ra.RouteId AND tr.IsDeleted=0
              JOIN route_stops rs ON rs.Id=ra.StopId AND rs.IsDeleted=0
              WHERE ra.TenantId=@TenantId AND ra.IsActive=1 AND ra.IsDeleted=0
                AND (@RouteId IS NULL OR ra.RouteId=@RouteId)
              ORDER BY tr.Name, rs.Sequence, ra.MemberName",
            new[] { "RouteName","MemberName","MemberType","StopName","Sequence" },
            new[] { new FilterDef("RouteId","Route","guid",false,null) })
    };
}

public record PredefinedReport(
    string Code, string Name, ReportCategory Category,
    string SqlQuery, string[] DefaultColumns, FilterDef[] Filters);

public record FilterDef(string Key, string DisplayName, string Type, bool IsRequired, string? DefaultValue);
