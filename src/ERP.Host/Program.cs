using System.Text;
using DinkToPdf;
using ERP.Transport.Application.Commands;
using ERP.Transport.Application.Jobs;
using ERP.Transport.Domain;
using ERP.Transport.Infrastructure;
using ERP.Accreditation.Application.Commands;
using ERP.Accreditation.Infrastructure;
using ERP.NAAC.Application.Commands;
using ERP.NAAC.Infrastructure;
using ERP.NIRF.Application.Commands;
using ERP.NIRF.Infrastructure;
using ERP.ABC.Application.Commands;
using ERP.ABC.Domain;
using ERP.ABC.Infrastructure;
using ERP.Research.Application.Commands;
using ERP.Research.Infrastructure;
using ERP.Alumni.Application.Commands;
using ERP.Alumni.Infrastructure;
using ERP.Chatbot.API.Hubs;
using ERP.Chatbot.Application.Commands;
using ERP.Chatbot.Application.Services;
using ERP.Chatbot.Application.Services.IntentHandlers;
using ERP.Chatbot.Infrastructure;
using ERP.Analytics.Application.Commands;
using ERP.Analytics.Application.Jobs;
using ERP.Analytics.Infrastructure;
using ERP.MobileApi.Application.Commands;
using ERP.MobileApi.Application.Services;
using ERP.MobileApi.Infrastructure;
using ERP.Reporting.Application.Commands;
using ERP.Reporting.Application.Jobs;
using ERP.Reporting.Application.Services;
using ERP.Reporting.Infrastructure;
using ERP.Compliance.Application.Commands;
using ERP.Compliance.Application.Jobs;
using ERP.Compliance.Infrastructure;
using ERP.OBE.Application.Commands;
using ERP.OBE.Infrastructure;
using ERP.Placement.Application.Commands;
using ERP.Placement.Infrastructure;
using ERP.HRMS.Application.Commands;
using ERP.HRMS.Infrastructure;
using ERP.Library.Application.Commands;
using ERP.Library.Application.Jobs;
using ERP.Library.Infrastructure;
using DinkToPdf.Contracts;
using ERP.Academics.Application.Commands;
using ERP.Academics.Application.Services;
using ERP.Academics.Infrastructure;
using ERP.Attendance.Application.Commands;
using ERP.Attendance.Application.Jobs;
using ERP.Exams.Application.Commands;
using ERP.Exams.Infrastructure;
using ERP.Fees.Application.Commands;
using ERP.Fees.Application.Services;
using ERP.Fees.Infrastructure;
using ERP.LMS.Application.Commands;
using ERP.LMS.API.Hubs;
using ERP.LMS.Infrastructure;
using ERP.Attendance.Infrastructure;
using ERP.Timetable.API.Hubs;
using ERP.Timetable.Application.Commands;
using ERP.Timetable.Application.Services;
using ERP.Timetable.Infrastructure;
using ERP.Admissions.Application.Commands;
using ERP.Admissions.Infrastructure;
using ERP.Auth.Application.Services;
using ERP.Auth.Infrastructure;
using ERP.Host;
using ERP.Host.Middleware;
using Hangfire.Dashboard;
using ERP.RBAC.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Contracts;
using ERP.Shared.Infrastructure;
using ERP.SIS.Application.Services;
using ERP.SIS.Infrastructure;
using FluentValidation;
using Hangfire;
using MediatR;
using Hangfire.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;

// Bootstrap logger used only until Host builds and reads appsettings
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ERP Platform API");

    var builder = WebApplication.CreateBuilder(args);

    // Render (and most PaaS) inject PORT — bind to 0.0.0.0 so the health-check probe can reach us.
    // Falls back to 8080 locally if PORT is not set (launchSettings handles local dev separately).
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

    builder.Host.UseSerilog((ctx, services, loggerConfig) =>
    {
        loggerConfig
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "ERP.Platform")
            .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName);

        var aiConnStr = ctx.Configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(aiConnStr))
        {
            // Route Serilog events as App Insights Trace telemetry; the SDK's automatic
            // exception tracking captures exception telemetry separately to avoid double-billing.
            loggerConfig.WriteTo.ApplicationInsights(
                services.GetRequiredService<Microsoft.ApplicationInsights.TelemetryClient>(),
                TelemetryConverter.Traces);
        }
    });

    // Controllers
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Azure App Insights — silently no-ops when ConnectionString is absent so dev environments need no config
    var appInsightsConnStr = builder.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrWhiteSpace(appInsightsConnStr))
    {
        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = appInsightsConnStr;
            options.EnablePerformanceCounterCollectionModule = true;
            options.EnableRequestTrackingTelemetryModule = true;
            options.EnableDependencyTrackingTelemetryModule = true;
        });
    }

    // Swagger
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "ERP Platform API", Version = "v1" });
        c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer {token}'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    // Database - EF Core + MySQL
    var writeConnStr = builder.Configuration.GetConnectionString("Write")!;
        builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    {
        // Use a hardcoded version instead of AutoDetect to avoid a synchronous
        // TCP connection to MySQL at DI registration time — AutoDetect crashes
        // if the database is temporarily unreachable during startup.
        var mysqlVersion = new MySqlServerVersion(new Version(8, 0, 0));
        options.UseMySql(writeConnStr, mysqlVersion,
            mysqlOptions =>
            {
                mysqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
                mysqlOptions.CommandTimeout(60);
            });
    });

    // Register AppDbContext as all the interfaces it implements
    builder.Services.AddScoped<ERP.Tenants.Application.Commands.ITenantsDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<ERP.Tenants.Application.Queries.ITenantsReadDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<ERP.Auth.Application.Commands.IAuthDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IRbacDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<ERP.Users.Infrastructure.IUsersDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IAdmissionsDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<ISisDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IAcademicsDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<ITimetableDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IAttendanceDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IExamsDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<ILmsDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IFeesDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<ERP.Finance.Infrastructure.IFinanceDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IHrmsDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<ILibraryDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<ERP.Hostel.Infrastructure.IHostelDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<ITransportDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IPlacementDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IAccreditationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<INaacDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IObeDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<INirfDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IComplianceDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IAbcDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IResearchDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IAlumniDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IChatbotDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IAnalyticsDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IMobileDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IReportingDbContext>(sp => sp.GetRequiredService<AppDbContext>());
    builder.Services.AddScoped<IReportExporter, PdfReportExporter>();
    builder.Services.AddScoped<IReportExporter, ExcelReportExporter>();
    builder.Services.AddScoped<IReportExporter, CsvReportExporter>();
    builder.Services.AddScoped<IPushNotificationService, NullPushNotificationService>();
    builder.Services.AddScoped<ILlmService, RuleBasedIntentRouter>();
    builder.Services.AddScoped<FeeBalanceIntentHandler>();
    builder.Services.AddScoped<ExamScheduleIntentHandler>();
    builder.Services.AddScoped<TimetableIntentHandler>();
    builder.Services.AddScoped<AttendanceSummaryIntentHandler>();
    builder.Services.AddScoped<FacultyBriefIntentHandler>();
    builder.Services.AddScoped<KpiQueryIntentHandler>();
    builder.Services.AddScoped<IDigiLockerService, NullDigiLockerService>();

    // Evidence providers — all implementations collected via IEnumerable<IEvidenceProvider>
    builder.Services.AddScoped<IEvidenceProvider, ERP.SIS.Application.Services.StudentEnrollmentEvidenceProvider>();
    builder.Services.AddScoped<IEvidenceProvider, ERP.Exams.Application.Services.ExamResultEvidenceProvider>();
    builder.Services.AddScoped<IEvidenceProvider, ERP.Attendance.Application.Services.AttendanceEvidenceProvider>();
    builder.Services.AddScoped<IEvidenceProvider, ERP.Finance.Application.Services.FeeCollectionEvidenceProvider>();
    builder.Services.AddScoped<IGpsProvider, NullGpsProvider>();
    builder.Services.AddScoped<PayrollCalculatorService>();
    builder.Services.AddScoped<ERP.Shared.Application.Contracts.IFeeService, FeeService>();
    builder.Services.AddScoped<LateFineCalculatorService>();
    builder.Services.AddScoped<TimetableGeneratorService>();
    builder.Services.AddScoped<ERP.Shared.Application.Contracts.ISubjectService, SubjectService>();

    // Redis — abortConnect=false lets the app start even when Redis is temporarily unavailable.
    // The multiplexer will keep retrying in the background; individual cache calls will fail fast
    // (and the caller handles the miss) rather than crashing the host at startup.
    var redisConnStr = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
    var redisConfig = ConfigurationOptions.Parse(redisConnStr);
    redisConfig.AbortOnConnectFail = false;          // don't throw on startup if Redis is down
    redisConfig.ConnectRetry = 5;
    redisConfig.ReconnectRetryPolicy = new ExponentialRetry(5000);
    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<Program>>();
        var mux = ConnectionMultiplexer.Connect(redisConfig);
        mux.ConnectionFailed  += (_, e) => logger.LogWarning("Redis connection failed: {Endpoint} — {FailureType}", e.EndPoint, e.FailureType);
        mux.ConnectionRestored += (_, e) => logger.LogInformation("Redis connection restored: {Endpoint}", e.EndPoint);
        return mux;
    });
    builder.Services.AddScoped<ICacheService, RedisCacheService>();

    // Tenant & User context
    builder.Services.AddScoped<ICurrentTenant, CurrentTenant>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();

    // Connection factory
    builder.Services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();

    // MediatR - scan all module assemblies
    builder.Services.AddMediatR(cfg =>
    {
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ERP.Host.Behaviours.LoggingPipelineBehaviour<,>));
        cfg.RegisterServicesFromAssemblies(
            typeof(Program).Assembly,
            typeof(ERP.Tenants.Application.Commands.CreateTenantCommand).Assembly,
            typeof(ERP.Auth.Application.Commands.LoginCommand).Assembly,
            typeof(ERP.RBAC.Application.Commands.CreateRoleCommand).Assembly,
            typeof(ERP.Users.Application.Commands.CreateUserCommand).Assembly,
            typeof(ERP.Admissions.Application.Commands.SubmitApplicationCommand).Assembly,
            typeof(ERP.SIS.Application.Events.StudentEnrolledEventHandler).Assembly,
            typeof(CreateDepartmentCommand).Assembly,
            typeof(GenerateTimetableCommand).Assembly,
            typeof(CreateSessionCommand).Assembly,
            typeof(CreateExamScheduleCommand).Assembly,
            typeof(UploadContentCommand).Assembly,
            typeof(CreateFeeStructureCommand).Assembly,
            typeof(ERP.Finance.Application.Commands.CreateAccountCommand).Assembly,
            typeof(CreateEmployeeCommand).Assembly,
            typeof(AddBookCommand).Assembly,
            typeof(ERP.Hostel.Application.Commands.CreateBlockCommand).Assembly,
            typeof(CreateRouteCommand).Assembly,
            typeof(CreateCompanyCommand).Assembly,
            typeof(TagEvidenceCommand).Assembly,
            typeof(CreateSsrCommand).Assembly,
            typeof(SetCoPoMappingCommand).Assembly,
            typeof(CompileNirfDataCommand).Assembly,
            typeof(CreateComplianceItemCommand).Assembly,
            typeof(LinkAbcIdCommand).Assembly,
            typeof(CreateResearchProjectCommand).Assembly,
            typeof(RegisterAlumniCommand).Assembly,
            typeof(SendMessageCommand).Assembly,
            typeof(ComputeAtRiskScoresCommand).Assembly,
            typeof(RegisterDeviceCommand).Assembly,
            typeof(ExecuteReportCommand).Assembly
        );
    });

    // FluentValidation
    builder.Services.AddValidatorsFromAssemblies(new[]
    {
        typeof(ERP.Tenants.API.Dtos.CreateTenantDtoValidator).Assembly,
        typeof(ERP.Auth.API.Dtos.LoginRequestValidator).Assembly,
        typeof(ERP.Users.API.Dtos.CreateUserDtoValidator).Assembly
    });

    // Shared infrastructure services
    builder.Services.AddSingleton<IConverter, SynchronizedConverter>(_ => new SynchronizedConverter(new PdfTools()));
    builder.Services.AddScoped<IPdfService, HtmlToPdfService>();
    builder.Services.AddScoped<IAzureBlobService, NullAzureBlobService>();
    builder.Services.AddScoped<IEncryptionService, EncryptionService>();
    builder.Services.AddScoped<IStudentEnrollmentService, StudentEnrollmentService>();

    // Auth services
    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddScoped<ITotpService, TotpService>();
    builder.Services.AddScoped<ISmsService, NullSmsService>();
    builder.Services.AddScoped<IWhatsAppService, NullWhatsAppService>();
    builder.Services.AddScoped<PermissionCacheService>();
    builder.Services.AddScoped<ERP.Shared.Application.Contracts.IPermissionService>(sp => sp.GetRequiredService<PermissionCacheService>());
    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

    // JWT Authentication
    var jwtKey = builder.Configuration["Jwt:Key"]!;
    var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
    var jwtAudience = builder.Configuration["Jwt:Audience"]!;

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    // Authorization policies
    builder.Services.AddAuthorization(options =>
    {
        // Permission-based policies
        var permissions = new[]
        {
            "users:read", "users:write",
            "roles:read", "roles:write",
            "tenants:read", "tenants:write"
        };

        foreach (var permission in permissions)
        {
            options.AddPolicy(permission,
                policy => policy.AddRequirements(new PermissionRequirement(permission)));
        }
    });

    // Hangfire
    builder.Services.AddHangfire(config =>
        config.UseStorage(new MySqlStorage(writeConnStr, new MySqlStorageOptions
        {
            TablesPrefix = "Hangfire_",
            PrepareSchemaIfNecessary = true
        })));
    builder.Services.AddHangfireServer();

    builder.Services.AddSignalR();

    // CORS
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? Array.Empty<string>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultPolicy", policy =>
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
    });

    var app = builder.Build();

    // Run migrations and seed data on startup
    using (var scope = app.Services.CreateScope())
    {
        var dbContext     = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        await ApplyMigrationsResiliently(dbContext, startupLogger);

        await DatabaseSeeder.SeedAsync(dbContext, startupLogger);

        // Seed predefined reports for all existing tenants
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var tenantIds = await dbContext.Tenants.Select(t => t.Id).ToListAsync();
        foreach (var tenantId in tenantIds)
            await mediator.Send(new SeedPredefinedReportsCommand(tenantId));
    }

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Enable Swagger in all non-production environments AND when explicitly enabled via config.
    // Set EnableSwagger=true in Render environment variables to expose docs on production.
    var enableSwagger = app.Environment.IsDevelopment()
                     || string.Equals(builder.Configuration["EnableSwagger"], "true",
                            StringComparison.OrdinalIgnoreCase);

    if (enableSwagger)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP Platform API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault() ?? "");
            diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        };
    });

    app.UseCors("DefaultPolicy");
    app.UseAuthentication();
    app.UseMiddleware<TenantResolutionMiddleware>();
    app.UseAuthorization();

    app.UseHangfireDashboard(builder.Configuration["Hangfire:DashboardPath"] ?? "/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthFilter() }
    });

    // Register recurring jobs after the Hangfire background server has started
    // and created its schema tables — otherwise AddOrUpdate throws on a cold DB.
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        RecurringJob.AddOrUpdate<AttendanceAlertJob>(
            "attendance-below-75-alert",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(8));

        RecurringJob.AddOrUpdate<OverdueNotificationJob>(
            "library-overdue-notify",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(9));

        RecurringJob.AddOrUpdate<ERP.Transport.Application.Jobs.ComplianceAlertJob>(
            "transport-compliance-alert",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(7));

        RecurringJob.AddOrUpdate<ERP.Compliance.Application.Jobs.ComplianceAlertJob>(
            "compliance-deadline-alert",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(7));

        RecurringJob.AddOrUpdate<AnalyticsRefreshJob>(
            "analytics-weekly-refresh",
            job => job.RunAsync(CancellationToken.None),
            "0 6 * * MON");

        RecurringJob.AddOrUpdate<ScheduledReportJob>(
            "scheduled-reports",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly);
    });

    app.MapControllers();
    app.MapHub<TimetableHub>("/hubs/timetable");
    app.MapHub<ForumHub>("/hubs/forum");
    app.MapHub<ChatHub>("/hubs/chat");

    app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow })
        .AllowAnonymous();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// ── Resilient migration runner ────────────────────────────────────────────────
// Retries MigrateAsync() until all migrations are applied. When a migration fails
// with MySQL error 1050 ("table already exists") the table was created in a run
// before Designer.cs tracking existed, so EF doesn't know it ran. We mark it
// applied in __EFMigrationsHistory and retry — no manual DB intervention needed.
static async Task ApplyMigrationsResiliently(
    AppDbContext db, Microsoft.Extensions.Logging.ILogger logger)
{
    const int maxAttempts = 60; // guard against infinite loop on genuine errors

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("All migrations applied successfully.");
            return;
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1118)
        {
            // Row size too large — a column that should be TEXT is still VARCHAR.
            // This means a migration needs its large varchar columns converted to longtext.
            logger.LogCritical(ex,
                "Migration failed: row size too large in the target table. " +
                "Ensure all varchar(≥500) columns use longtext in the migration file.");
            throw;
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1050)
        {
            // Identify the first pending migration that caused the conflict and mark it
            // applied so MigrateAsync() can advance past it on the next attempt.
            var firstPending = (await db.Database.GetPendingMigrationsAsync())
                               .FirstOrDefault();

            if (firstPending is null)
            {
                logger.LogWarning("MySqlException 1050 but no pending migrations found — rethrowing.");
                throw;
            }

            logger.LogWarning(
                "Migration {Id} skipped (tables already exist from a prior untracked run). " +
                "Recording as applied. Attempt {Attempt}/{Max}.",
                firstPending, attempt, maxAttempts);

            var sql = $"INSERT IGNORE INTO `__EFMigrationsHistory` " +
                      $"(`MigrationId`, `ProductVersion`) VALUES " +
                      $"('{firstPending.Replace("'", "''")}', '8.0.13')";

            await db.Database.ExecuteSqlRawAsync(sql);
        }
    }

    throw new InvalidOperationException(
        $"Could not apply all migrations after {maxAttempts} attempts.");
}
