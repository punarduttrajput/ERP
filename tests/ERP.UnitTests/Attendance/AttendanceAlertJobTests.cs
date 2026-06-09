using System.Data;
using Dapper;
using ERP.Attendance.Application.Jobs;
using ERP.Shared.Application.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERP.UnitTests.Attendance;

public class AttendanceAlertJobTests
{
    [Fact]
    public async Task Below75Percent_SendsSms()
    {
        // 2 out of 4 sessions present = 50% → below 75 → SMS must be sent
        var tenantId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        const string mobile = "+91-9000000001";

        var mockConn = new Mock<IDbConnection>();
        var mockFactory = new Mock<IDbConnectionFactory>();
        var mockSms = new Mock<ISmsService>();
        var mockCache = new Mock<ICacheService>();

        // Step 1: return one tenant+semester combo
        // Step 2: return one student+subject row below 75%
        // Step 3: return a mobile number for the student
        var callCount = 0;
        mockConn.Setup(c => c.QueryAsync<dynamic>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), It.IsAny<int?>(), It.IsAny<CommandType?>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    return Task.FromResult<IEnumerable<dynamic>>(new[] { new { TenantId = tenantId, SemesterId = semesterId } }.AsEnumerable<dynamic>());
                return Task.FromResult<IEnumerable<dynamic>>(new[] { new { StudentId = studentId, SubjectId = subjectId, TotalSessions = 4, PresentCount = 2 } }.AsEnumerable<dynamic>());
            });

        mockConn.Setup(c => c.QueryFirstOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), It.IsAny<int?>(), It.IsAny<CommandType?>()))
            .ReturnsAsync(mobile);

        mockFactory.Setup(f => f.CreateReadConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockConn.Object);

        mockCache.Setup(c => c.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var job = new AttendanceAlertJob(mockFactory.Object, mockSms.Object, mockCache.Object, NullLogger<AttendanceAlertJob>.Instance);

        await job.RunAsync(CancellationToken.None);

        mockSms.Verify(s => s.SendAsync(mobile, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        mockCache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), TimeSpan.FromDays(7), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Above75Percent_NoSms()
    {
        // 4 out of 4 sessions present = 100% → no alert
        var tenantId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();

        var mockConn = new Mock<IDbConnection>();
        var mockFactory = new Mock<IDbConnectionFactory>();
        var mockSms = new Mock<ISmsService>();
        var mockCache = new Mock<ICacheService>();

        var callCount = 0;
        mockConn.Setup(c => c.QueryAsync<dynamic>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<IDbTransaction>(), It.IsAny<int?>(), It.IsAny<CommandType?>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    return Task.FromResult<IEnumerable<dynamic>>(new[] { new { TenantId = tenantId, SemesterId = semesterId } }.AsEnumerable<dynamic>());
                // Empty result: nobody below 75%
                return Task.FromResult<IEnumerable<dynamic>>(Enumerable.Empty<dynamic>());
            });

        mockFactory.Setup(f => f.CreateReadConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockConn.Object);

        var job = new AttendanceAlertJob(mockFactory.Object, mockSms.Object, mockCache.Object, NullLogger<AttendanceAlertJob>.Instance);

        await job.RunAsync(CancellationToken.None);

        mockSms.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
