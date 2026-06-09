using ERP.Analytics.Application.Commands;
using ERP.Analytics.Domain;
using FluentAssertions;
using Xunit;

namespace ERP.UnitTests.Analytics;

public class AnalyticsTests
{
    [Fact]
    public void AtRiskScore_LowAttendanceLowMarks_Critical()
    {
        var (score, level, attFlag, marksFlag) = ComputeAtRiskScoresHandler.ComputeAtRiskScore(60m, 40m);

        level.Should().Be(RiskLevel.Critical);
        attFlag.Should().BeTrue();
        marksFlag.Should().BeTrue();
    }

    [Fact]
    public void AtRiskScore_HighAttendanceHighMarks_Low()
    {
        var (score, level, attFlag, marksFlag) = ComputeAtRiskScoresHandler.ComputeAtRiskScore(90m, 85m);

        level.Should().Be(RiskLevel.Low);
        attFlag.Should().BeFalse();
        marksFlag.Should().BeFalse();
    }

    [Fact]
    public void AtRiskScore_Flags_SetCorrectly()
    {
        var (score, level, attFlag, marksFlag) = ComputeAtRiskScoresHandler.ComputeAtRiskScore(70m, 45m);

        attFlag.Should().BeTrue();
        marksFlag.Should().BeTrue();
        var combinedFlag = attFlag && marksFlag;
        combinedFlag.Should().BeTrue();
    }

    [Fact]
    public void FeeDefaultRisk_LargeOverdue_HighRisk()
    {
        var (score, level) = ComputeFeeDefaultRiskHandler.ComputeFeeDefaultScore(50000m, 60, 2);

        level.Should().BeOneOf(RiskLevel.High, RiskLevel.Critical);
    }

    [Fact]
    public void FeeDefaultRisk_NoOverdue_Low()
    {
        var (score, level) = ComputeFeeDefaultRiskHandler.ComputeFeeDefaultScore(5000m, 0, 0);

        level.Should().Be(RiskLevel.Low);
    }

    [Fact]
    public void PlacementScore_HighCgpaNoBacklogs_85Percent()
    {
        var (score, probability) = ComputePlacementScoresHandler.ComputePlacementScore(9.0m, 0, 95m);

        probability.Should().Be(85m);
    }

    [Fact]
    public void PlacementScore_LowCgpaWithBacklogs_20Percent()
    {
        var (score, probability) = ComputePlacementScoresHandler.ComputePlacementScore(5.0m, 3, 60m);

        probability.Should().Be(20m);
    }
}
