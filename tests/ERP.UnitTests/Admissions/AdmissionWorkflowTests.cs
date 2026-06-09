using ERP.Admissions.Domain;
using FluentAssertions;
using Xunit;

namespace ERP.UnitTests.Admissions;

public class AdmissionWorkflowTests
{
    private static AdmissionApplication BuildApp(ApplicationState state = ApplicationState.Draft) => new()
    {
        Id = Guid.NewGuid(), TenantId = Guid.NewGuid(),
        ApplicantName = "Test", ApplicantEmail = "t@t.com",
        ApplicantMobile = "+911234567890", ProgramId = Guid.NewGuid(),
        ProgramName = "CS", Category = "General", AcademicYear = 2026,
        State = state, WorkflowDefinitionId = Guid.NewGuid(), WorkflowDefinitionVersion = 1
    };

    [Theory]
    [InlineData(ApplicationState.Draft, ApplicationState.Submitted, true)]
    [InlineData(ApplicationState.Submitted, ApplicationState.UnderVerification, true)]
    [InlineData(ApplicationState.UnderVerification, ApplicationState.Verified, true)]
    [InlineData(ApplicationState.UnderVerification, ApplicationState.Rejected, true)]
    [InlineData(ApplicationState.Verified, ApplicationState.MeritEvaluated, true)]
    [InlineData(ApplicationState.MeritEvaluated, ApplicationState.OfferMade, true)]
    [InlineData(ApplicationState.OfferMade, ApplicationState.OfferAccepted, true)]
    [InlineData(ApplicationState.OfferAccepted, ApplicationState.Enrolled, true)]
    [InlineData(ApplicationState.Draft, ApplicationState.Withdrawn, true)]
    [InlineData(ApplicationState.OfferMade, ApplicationState.Withdrawn, true)]
    [InlineData(ApplicationState.Enrolled, ApplicationState.Withdrawn, false)]
    [InlineData(ApplicationState.Rejected, ApplicationState.Submitted, false)]
    [InlineData(ApplicationState.Draft, ApplicationState.Enrolled, false)]
    public void CanTransitionTo_ReturnsExpected(ApplicationState from, ApplicationState to, bool expected)
    {
        var app = BuildApp(from);
        app.CanTransitionTo(to).Should().Be(expected);
    }

    [Fact]
    public void Transition_ValidTransition_ChangesStateAndAddsAuditEntry()
    {
        var app    = BuildApp(ApplicationState.Draft);
        var actor  = Guid.NewGuid();

        app.Transition(ApplicationState.Submitted, actor, "Submitted by applicant");

        app.State.Should().Be(ApplicationState.Submitted);
        app.AuditEntries.Should().HaveCount(1);
        app.AuditEntries.First().FromState.Should().Be(ApplicationState.Draft);
        app.AuditEntries.First().ToState.Should().Be(ApplicationState.Submitted);
        app.AuditEntries.First().ActorId.Should().Be(actor);
        app.AuditEntries.First().Reason.Should().Be("Submitted by applicant");
    }

    [Fact]
    public void Transition_InvalidTransition_Throws()
    {
        var app = BuildApp(ApplicationState.Enrolled);

        var act = () => app.Transition(ApplicationState.Draft, Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>().WithMessage("*Enrolled*Draft*");
    }

    [Fact]
    public void Transition_MultipleTransitions_BuildsFullAuditTrail()
    {
        var app = BuildApp(ApplicationState.Draft);
        var actor = Guid.NewGuid();

        app.Transition(ApplicationState.Submitted, actor, "Submitted");
        app.Transition(ApplicationState.UnderVerification, actor, "Verification started");
        app.Transition(ApplicationState.Verified, actor, "Docs verified");

        app.State.Should().Be(ApplicationState.Verified);
        app.AuditEntries.Should().HaveCount(3);
    }

    [Fact]
    public void Transition_OfferAcceptedToEnrolled_SetsEnrollmentPath()
    {
        var app = BuildApp(ApplicationState.OfferMade);
        var actor = Guid.NewGuid();

        app.Transition(ApplicationState.OfferAccepted, actor, "Accepted");
        app.Transition(ApplicationState.Enrolled, actor, "Confirmed");

        app.State.Should().Be(ApplicationState.Enrolled);
        app.AuditEntries.Should().HaveCount(2);
    }
}
