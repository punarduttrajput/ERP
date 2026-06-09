using BCrypt.Net;
using ERP.Auth.Domain;
using ERP.RBAC.Domain;
using ERP.Tenants.Domain;
using ERP.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Host;

/// <summary>
/// Seeds all reference and bootstrap data into the database on first run.
/// Idempotent — safe to call every startup; skips records that already exist.
/// </summary>
public static class DatabaseSeeder
{
    // ── Fixed GUIDs so FK relationships are stable across re-seeds ────────
    private static readonly Guid SystemTenantId   = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DemoTenantId     = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid SuperAdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private static readonly Guid AdminUserId      = Guid.Parse("00000000-0000-0000-0000-000000000011");
    private static readonly Guid FacultyUserId    = Guid.Parse("00000000-0000-0000-0000-000000000012");
    private static readonly Guid StudentUserId    = Guid.Parse("00000000-0000-0000-0000-000000000013");

    private static readonly Guid RoleSuperAdminId = Guid.Parse("00000000-0000-0000-0001-000000000001");
    private static readonly Guid RoleAdminId      = Guid.Parse("00000000-0000-0000-0001-000000000002");
    private static readonly Guid RoleFacultyId    = Guid.Parse("00000000-0000-0000-0001-000000000003");
    private static readonly Guid RoleStudentId    = Guid.Parse("00000000-0000-0000-0001-000000000004");
    private static readonly Guid RoleStaffId      = Guid.Parse("00000000-0000-0000-0001-000000000005");

    // ── All permission GUIDs ───────────────────────────────────────────────
    private static readonly Dictionary<string, Guid> PermIds = new()
    {
        // Tenant management
        ["tenants:read"]       = Guid.Parse("00000000-0000-0000-0002-000000000001"),
        ["tenants:write"]      = Guid.Parse("00000000-0000-0000-0002-000000000002"),
        ["tenants:delete"]     = Guid.Parse("00000000-0000-0000-0002-000000000003"),
        // User management
        ["users:read"]         = Guid.Parse("00000000-0000-0000-0002-000000000010"),
        ["users:write"]        = Guid.Parse("00000000-0000-0000-0002-000000000011"),
        ["users:delete"]       = Guid.Parse("00000000-0000-0000-0002-000000000012"),
        // Role management
        ["roles:read"]         = Guid.Parse("00000000-0000-0000-0002-000000000020"),
        ["roles:write"]        = Guid.Parse("00000000-0000-0000-0002-000000000021"),
        ["roles:delete"]       = Guid.Parse("00000000-0000-0000-0002-000000000022"),
        // Academics
        ["academics:read"]     = Guid.Parse("00000000-0000-0000-0002-000000000030"),
        ["academics:write"]    = Guid.Parse("00000000-0000-0000-0002-000000000031"),
        // Admissions
        ["admissions:read"]    = Guid.Parse("00000000-0000-0000-0002-000000000040"),
        ["admissions:write"]   = Guid.Parse("00000000-0000-0000-0002-000000000041"),
        // Finance
        ["finance:read"]       = Guid.Parse("00000000-0000-0000-0002-000000000050"),
        ["finance:write"]      = Guid.Parse("00000000-0000-0000-0002-000000000051"),
        // HR
        ["hr:read"]            = Guid.Parse("00000000-0000-0000-0002-000000000060"),
        ["hr:write"]           = Guid.Parse("00000000-0000-0000-0002-000000000061"),
        // Examinations
        ["exams:read"]         = Guid.Parse("00000000-0000-0000-0002-000000000070"),
        ["exams:write"]        = Guid.Parse("00000000-0000-0000-0002-000000000071"),
        // Library
        ["library:read"]       = Guid.Parse("00000000-0000-0000-0002-000000000080"),
        ["library:write"]      = Guid.Parse("00000000-0000-0000-0002-000000000081"),
        // Reports
        ["reports:read"]       = Guid.Parse("00000000-0000-0000-0002-000000000090"),
        ["reports:write"]      = Guid.Parse("00000000-0000-0000-0002-000000000091"),
        // Hostel
        ["hostel:read"]        = Guid.Parse("00000000-0000-0000-0002-000000000100"),
        ["hostel:write"]       = Guid.Parse("00000000-0000-0000-0002-000000000101"),
        // Dashboard
        ["dashboard:read"]     = Guid.Parse("00000000-0000-0000-0002-000000000110"),
    };

    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync())
        {
            logger.LogInformation("Database already seeded, skipping.");
            return;
        }

        logger.LogInformation("Starting database seed...");

        await SeedTenantsAsync(db, logger);
        await SeedPermissionsAsync(db, logger);
        await SeedRolesAsync(db, logger);
        await SeedRolePermissionsAsync(db, logger);
        await SeedUsersAsync(db, logger);
        await SeedUserRolesAsync(db, logger);
        await SeedMenuItemsAsync(db, logger);

        logger.LogInformation("Database seed completed.");
    }

    // ── 1. Tenants ─────────────────────────────────────────────────────────
    private static async Task SeedTenantsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Id == SystemTenantId))
            return;

        var tenants = new List<Tenant>
        {
            new()
            {
                Id           = SystemTenantId,
                Name         = "System Administration",
                Slug         = "system",
                Status       = TenantStatus.Active,
                ContactEmail = "admin@erp-platform.com",
                Plan         = "enterprise",
                PrimaryColor = "#1E3A5F",
                SecondaryColor = "#00B4D8",
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow
            },
            new()
            {
                Id           = DemoTenantId,
                Name         = "MIT University (Demo)",
                Slug         = "mit",
                Status       = TenantStatus.Active,
                ContactEmail = "admin@mit.edu",
                ContactPhone = "+1-617-000-0000",
                Address      = "77 Massachusetts Ave, Cambridge, MA",
                Plan         = "enterprise",
                PrimaryColor = "#A31F34",
                SecondaryColor = "#8A8B8C",
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow
            }
        };

        await db.Tenants.AddRangeAsync(tenants);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} tenants", tenants.Count);
    }

    // ── 2. Permissions ─────────────────────────────────────────────────────
    private static async Task SeedPermissionsAsync(AppDbContext db, ILogger logger)
    {
        var firstPermId = PermIds["tenants:read"];
        if (await db.Set<Permission>().IgnoreQueryFilters().AnyAsync(p => p.Id == firstPermId))
            return;

        var permDefs = new[]
        {
            // module, action, description
            ("tenants",    "read",   "View tenants"),
            ("tenants",    "write",  "Create and update tenants"),
            ("tenants",    "delete", "Delete tenants"),
            ("users",      "read",   "View users"),
            ("users",      "write",  "Create and update users"),
            ("users",      "delete", "Deactivate users"),
            ("roles",      "read",   "View roles and permissions"),
            ("roles",      "write",  "Create and update roles"),
            ("roles",      "delete", "Delete roles"),
            ("academics",  "read",   "View academic data"),
            ("academics",  "write",  "Manage academic data"),
            ("admissions", "read",   "View admissions"),
            ("admissions", "write",  "Manage admissions"),
            ("finance",    "read",   "View financial records"),
            ("finance",    "write",  "Manage financial records"),
            ("hr",         "read",   "View HR data"),
            ("hr",         "write",  "Manage HR data"),
            ("exams",      "read",   "View examinations"),
            ("exams",      "write",  "Manage examinations"),
            ("library",    "read",   "View library resources"),
            ("library",    "write",  "Manage library resources"),
            ("reports",    "read",   "View reports"),
            ("reports",    "write",  "Generate reports"),
            ("hostel",     "read",   "View hostel data"),
            ("hostel",     "write",  "Manage hostel data"),
            ("dashboard",  "read",   "View dashboard"),
        };

        var permissions = permDefs.Select(p =>
        {
            var key = $"{p.Item1}:{p.Item2}";
            return new Permission
            {
                Id          = PermIds[key],
                TenantId    = SystemTenantId,
                Name        = key,
                Module      = p.Item1,
                Action      = p.Item2,
                Description = p.Item3,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            };
        }).ToList();

        // Also seed same permissions for the demo tenant
        var demoPermissions = permissions.Select(p => new Permission
        {
            Id          = Guid.NewGuid(),
            TenantId    = DemoTenantId,
            Name        = p.Name,
            Module      = p.Module,
            Action      = p.Action,
            Description = p.Description,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow
        }).ToList();

        await db.Set<Permission>().AddRangeAsync(permissions);
        await db.Set<Permission>().AddRangeAsync(demoPermissions);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} permissions", permissions.Count + demoPermissions.Count);
    }

    // ── 3. Roles ───────────────────────────────────────────────────────────
    private static async Task SeedRolesAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Set<Role>().IgnoreQueryFilters().AnyAsync(r => r.Id == RoleSuperAdminId))
            return;

        var roles = new List<Role>
        {
            new() { Id = RoleSuperAdminId, TenantId = SystemTenantId, Name = "Super Admin",   Description = "Full platform access — system administration only", IsSystemRole = true,  CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = RoleAdminId,      TenantId = DemoTenantId,   Name = "Admin",          Description = "Full access within the tenant",                      IsSystemRole = true,  CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = RoleFacultyId,    TenantId = DemoTenantId,   Name = "Faculty",        Description = "Academic staff — teaching and grading",               IsSystemRole = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = RoleStudentId,    TenantId = DemoTenantId,   Name = "Student",        Description = "Student — view own academic and financial data",      IsSystemRole = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = RoleStaffId,      TenantId = DemoTenantId,   Name = "Staff",          Description = "Administrative staff",                                IsSystemRole = false, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        };

        await db.Set<Role>().AddRangeAsync(roles);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} roles", roles.Count);
    }

    // ── 4. Role → Permission mappings ──────────────────────────────────────
    private static async Task SeedRolePermissionsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Set<RolePermission>().IgnoreQueryFilters().AnyAsync(rp => rp.RoleId == RoleSuperAdminId))
            return;

        var allSystemPermIds = PermIds.Values.ToList();

        // Super Admin: all permissions in system tenant
        var superAdminPerms = allSystemPermIds.Select(pid => new RolePermission
        {
            Id           = Guid.NewGuid(),
            TenantId     = SystemTenantId,
            RoleId       = RoleSuperAdminId,
            PermissionId = pid,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        }).ToList();

        // For demo tenant permissions, load their IDs
        var demoPermissions = await db.Set<Permission>()
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == DemoTenantId)
            .ToListAsync();

        Guid? DemoPermId(string name) =>
            demoPermissions.FirstOrDefault(p => p.Name == name)?.Id;

        // Admin: all demo permissions
        var adminPerms = demoPermissions.Select(p => new RolePermission
        {
            Id           = Guid.NewGuid(),
            TenantId     = DemoTenantId,
            RoleId       = RoleAdminId,
            PermissionId = p.Id,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow
        }).ToList();

        // Faculty: academics, exams, library (read+write), reports read, dashboard read
        var facultyPermNames = new[] { "academics:read","academics:write","exams:read","exams:write","library:read","reports:read","dashboard:read" };
        var facultyPerms = facultyPermNames
            .Select(n => DemoPermId(n))
            .Where(id => id.HasValue)
            .Select(id => new RolePermission
            {
                Id = Guid.NewGuid(), TenantId = DemoTenantId, RoleId = RoleFacultyId,
                PermissionId = id!.Value, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            }).ToList();

        // Student: own data views
        var studentPermNames = new[] { "academics:read","exams:read","library:read","finance:read","hostel:read","dashboard:read" };
        var studentPerms = studentPermNames
            .Select(n => DemoPermId(n))
            .Where(id => id.HasValue)
            .Select(id => new RolePermission
            {
                Id = Guid.NewGuid(), TenantId = DemoTenantId, RoleId = RoleStudentId,
                PermissionId = id!.Value, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            }).ToList();

        // Staff: admissions, hr, users read, reports read, dashboard read
        var staffPermNames = new[] { "admissions:read","admissions:write","hr:read","users:read","reports:read","dashboard:read" };
        var staffPerms = staffPermNames
            .Select(n => DemoPermId(n))
            .Where(id => id.HasValue)
            .Select(id => new RolePermission
            {
                Id = Guid.NewGuid(), TenantId = DemoTenantId, RoleId = RoleStaffId,
                PermissionId = id!.Value, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            }).ToList();

        await db.Set<RolePermission>().AddRangeAsync(superAdminPerms);
        await db.Set<RolePermission>().AddRangeAsync(adminPerms);
        await db.Set<RolePermission>().AddRangeAsync(facultyPerms);
        await db.Set<RolePermission>().AddRangeAsync(studentPerms);
        await db.Set<RolePermission>().AddRangeAsync(staffPerms);
        await db.SaveChangesAsync();

        var total = superAdminPerms.Count + adminPerms.Count + facultyPerms.Count + studentPerms.Count + staffPerms.Count;
        logger.LogInformation("Seeded {Count} role-permission mappings", total);
    }

    // ── 5. Users ───────────────────────────────────────────────────────────
    private static async Task SeedUsersAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == SuperAdminUserId))
            return;

        var now = DateTime.UtcNow;

        var users = new List<User>
        {
            new()
            {
                Id = SuperAdminUserId, TenantId = SystemTenantId,
                Email = "superadmin@erp-platform.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("SuperAdmin@123", workFactor: 12),
                FirstName = "Super", LastName = "Admin",
                IsActive = true, IsEmailVerified = true,
                CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                Id = AdminUserId, TenantId = DemoTenantId,
                Email = "admin@mit.edu",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123456", workFactor: 12),
                FirstName = "John", LastName = "Smith",
                IsActive = true, IsEmailVerified = true,
                CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                Id = FacultyUserId, TenantId = DemoTenantId,
                Email = "faculty@mit.edu",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Faculty@123456", workFactor: 12),
                FirstName = "Dr. Sarah", LastName = "Johnson",
                IsActive = true, IsEmailVerified = true,
                CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                Id = StudentUserId, TenantId = DemoTenantId,
                Email = "student@mit.edu",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@123456", workFactor: 12),
                FirstName = "Alice", LastName = "Williams",
                IsActive = true, IsEmailVerified = true,
                CreatedAt = now, UpdatedAt = now
            }
        };

        var profiles = new List<UserProfile>
        {
            new()
            {
                Id = SuperAdminUserId, TenantId = SystemTenantId,
                FirstName = "Super", LastName = "Admin",
                JobTitle = "Platform Administrator",
                Department = "IT",
                CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                Id = AdminUserId, TenantId = DemoTenantId,
                FirstName = "John", LastName = "Smith",
                JobTitle = "University Administrator",
                Department = "Administration",
                PhoneNumber = "+1-617-555-0100",
                CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                Id = FacultyUserId, TenantId = DemoTenantId,
                FirstName = "Dr. Sarah", LastName = "Johnson",
                JobTitle = "Associate Professor",
                Department = "Computer Science",
                PhoneNumber = "+1-617-555-0101",
                CreatedAt = now, UpdatedAt = now
            },
            new()
            {
                Id = StudentUserId, TenantId = DemoTenantId,
                FirstName = "Alice", LastName = "Williams",
                JobTitle = "B.Tech Student",
                Department = "Computer Science",
                PhoneNumber = "+1-617-555-0102",
                CreatedAt = now, UpdatedAt = now
            }
        };

        await db.Users.AddRangeAsync(users);
        await db.Set<UserProfile>().AddRangeAsync(profiles);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} users", users.Count);
    }

    // ── 6. User → Role mappings ────────────────────────────────────────────
    private static async Task SeedUserRolesAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Set<UserRole>().IgnoreQueryFilters().AnyAsync(ur => ur.UserId == SuperAdminUserId))
            return;

        var userRoles = new List<UserRole>
        {
            new() { Id = Guid.NewGuid(), TenantId = SystemTenantId, UserId = SuperAdminUserId, RoleId = RoleSuperAdminId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId,   UserId = AdminUserId,      RoleId = RoleAdminId,      CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId,   UserId = FacultyUserId,    RoleId = RoleFacultyId,    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId,   UserId = StudentUserId,    RoleId = RoleStudentId,    CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
        };

        await db.Set<UserRole>().AddRangeAsync(userRoles);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} user-role mappings", userRoles.Count);
    }

    // ── 7. Menu items ──────────────────────────────────────────────────────
    private static async Task SeedMenuItemsAsync(AppDbContext db, ILogger logger)
    {
        if (await db.Set<MenuItem>().IgnoreQueryFilters().AnyAsync(m => m.TenantId == DemoTenantId))
            return;

        var now = DateTime.UtcNow;

        // Top-level items (no parent)
        var dashboard   = new MenuItem { Id = Guid.NewGuid(), TenantId = DemoTenantId, Label = "Dashboard",   Icon = "dashboard",     Route = "/dashboard",           RequiredPermission = "dashboard:read",  Order = 1,  IsVisible = true, CreatedAt = now, UpdatedAt = now };
        var academics   = new MenuItem { Id = Guid.NewGuid(), TenantId = DemoTenantId, Label = "Academics",   Icon = "school",        Route = null,                   RequiredPermission = "academics:read",  Order = 2,  IsVisible = true, CreatedAt = now, UpdatedAt = now };
        var admissions  = new MenuItem { Id = Guid.NewGuid(), TenantId = DemoTenantId, Label = "Admissions",  Icon = "how_to_reg",    Route = null,                   RequiredPermission = "admissions:read", Order = 3,  IsVisible = true, CreatedAt = now, UpdatedAt = now };
        var finance     = new MenuItem { Id = Guid.NewGuid(), TenantId = DemoTenantId, Label = "Finance",     Icon = "account_balance",Route = null,                  RequiredPermission = "finance:read",    Order = 4,  IsVisible = true, CreatedAt = now, UpdatedAt = now };
        var hr          = new MenuItem { Id = Guid.NewGuid(), TenantId = DemoTenantId, Label = "HR & Payroll",Icon = "people",        Route = null,                   RequiredPermission = "hr:read",         Order = 5,  IsVisible = true, CreatedAt = now, UpdatedAt = now };
        var exams       = new MenuItem { Id = Guid.NewGuid(), TenantId = DemoTenantId, Label = "Examinations",Icon = "quiz",          Route = null,                   RequiredPermission = "exams:read",      Order = 6,  IsVisible = true, CreatedAt = now, UpdatedAt = now };
        var library     = new MenuItem { Id = Guid.NewGuid(), TenantId = DemoTenantId, Label = "Library",     Icon = "local_library", Route = "/library",             RequiredPermission = "library:read",    Order = 7,  IsVisible = true, CreatedAt = now, UpdatedAt = now };
        var hostel      = new MenuItem { Id = Guid.NewGuid(), TenantId = DemoTenantId, Label = "Hostel",      Icon = "hotel",         Route = "/hostel",              RequiredPermission = "hostel:read",     Order = 8,  IsVisible = true, CreatedAt = now, UpdatedAt = now };
        var reports     = new MenuItem { Id = Guid.NewGuid(), TenantId = DemoTenantId, Label = "Reports",     Icon = "bar_chart",     Route = "/reports",             RequiredPermission = "reports:read",    Order = 9,  IsVisible = true, CreatedAt = now, UpdatedAt = now };
        var settings    = new MenuItem { Id = Guid.NewGuid(), TenantId = DemoTenantId, Label = "Settings",    Icon = "settings",      Route = null,                   RequiredPermission = "roles:read",      Order = 10, IsVisible = true, CreatedAt = now, UpdatedAt = now };

        // Save parents first
        await db.Set<MenuItem>().AddRangeAsync(new[] { dashboard, academics, admissions, finance, hr, exams, library, hostel, reports, settings });
        await db.SaveChangesAsync();

        // Children
        var children = new List<MenuItem>
        {
            // Academics sub-menu
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = academics.Id,  Label = "Courses",         Icon = "menu_book",   Route = "/academics/courses",         RequiredPermission = "academics:read",  Order = 1, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = academics.Id,  Label = "Departments",     Icon = "business",    Route = "/academics/departments",     RequiredPermission = "academics:read",  Order = 2, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = academics.Id,  Label = "Timetable",       Icon = "calendar_today",Route = "/academics/timetable",     RequiredPermission = "academics:read",  Order = 3, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = academics.Id,  Label = "Attendance",      Icon = "fact_check",  Route = "/academics/attendance",      RequiredPermission = "academics:read",  Order = 4, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            // Admissions sub-menu
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = admissions.Id, Label = "Applications",    Icon = "description", Route = "/admissions/applications",   RequiredPermission = "admissions:read", Order = 1, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = admissions.Id, Label = "Enrolled Students",Icon = "groups",     Route = "/admissions/enrolled",       RequiredPermission = "admissions:read", Order = 2, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            // Finance sub-menu
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = finance.Id,    Label = "Fee Collection",  Icon = "payments",    Route = "/finance/fees",              RequiredPermission = "finance:read",    Order = 1, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = finance.Id,    Label = "Scholarships",    Icon = "workspace_premium",Route = "/finance/scholarships", RequiredPermission = "finance:read",    Order = 2, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = finance.Id,    Label = "Budget",          Icon = "account_balance_wallet",Route = "/finance/budget", RequiredPermission = "finance:read",    Order = 3, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            // HR sub-menu
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = hr.Id,         Label = "Employees",       Icon = "badge",       Route = "/hr/employees",              RequiredPermission = "hr:read",         Order = 1, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = hr.Id,         Label = "Payroll",         Icon = "attach_money",Route = "/hr/payroll",                RequiredPermission = "hr:read",         Order = 2, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = hr.Id,         Label = "Leave Management",Icon = "event_available",Route = "/hr/leave",               RequiredPermission = "hr:read",         Order = 3, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            // Exams sub-menu
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = exams.Id,      Label = "Schedule",        Icon = "event_note",  Route = "/exams/schedule",            RequiredPermission = "exams:read",      Order = 1, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = exams.Id,      Label = "Results",         Icon = "grade",       Route = "/exams/results",             RequiredPermission = "exams:read",      Order = 2, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = exams.Id,      Label = "Hall Tickets",    Icon = "confirmation_number",Route = "/exams/hall-tickets", RequiredPermission = "exams:read",      Order = 3, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            // Settings sub-menu
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = settings.Id,   Label = "Users",           Icon = "manage_accounts",Route = "/settings/users",         RequiredPermission = "users:read",      Order = 1, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = settings.Id,   Label = "Roles",           Icon = "admin_panel_settings",Route = "/settings/roles",    RequiredPermission = "roles:read",      Order = 2, IsVisible = true, CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), TenantId = DemoTenantId, ParentId = settings.Id,   Label = "Tenant Settings", Icon = "tune",        Route = "/settings/tenant",           RequiredPermission = "tenants:read",    Order = 3, IsVisible = true, CreatedAt = now, UpdatedAt = now },
        };

        await db.Set<MenuItem>().AddRangeAsync(children);
        await db.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} menu items", 10 + children.Count);
    }
}
