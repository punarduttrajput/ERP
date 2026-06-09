using ERP.Exams.Application.Events;
using ERP.Exams.Application.Services;
using ERP.Exams.Domain;
using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Application.Commands;

public record PublishResultsCommand(
    Guid TenantId,
    Guid SemesterId) : IRequest<Result<int>>;

public class PublishResultsHandler : IRequestHandler<PublishResultsCommand, Result<int>>
{
    private readonly IExamsDbContext _db;
    private readonly IMediator _mediator;

    public PublishResultsHandler(IExamsDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    public async Task<Result<int>> Handle(PublishResultsCommand request, CancellationToken cancellationToken)
    {
        var calculator = new GpaCalculatorService();

        // Load exam schedules for this semester
        var schedules = await _db.ExamSchedules
            .Where(s => s.SemesterId == request.SemesterId)
            .ToListAsync(cancellationToken);

        if (schedules.Count == 0)
            return Result<int>.Failure("No exam schedules found for this semester.");

        var scheduleIds = schedules.Select(s => s.Id).ToList();
        var scheduleMap = schedules.ToDictionary(s => s.Id);

        // Load external marks for the semester's schedules
        var externalMarks = await _db.ExternalMarks
            .Where(m => scheduleIds.Contains(m.ExamScheduleId))
            .ToListAsync(cancellationToken);

        // Load internal marks for the semester
        var internalMarks = await _db.InternalMarks
            .Where(m => m.SemesterId == request.SemesterId)
            .ToListAsync(cancellationToken);

        // Load default grading scheme
        var gradingScheme = await _db.GradingSchemes
            .Include(g => g.GradeRules)
            .Where(g => g.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);

        if (gradingScheme is null)
            return Result<int>.Failure("No default grading scheme found. Please create a grading scheme first.");

        var gradeRules = gradingScheme.GradeRules.ToList();

        // Group external marks by studentId
        var externalByStudent = externalMarks
            .GroupBy(m => m.StudentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Group internal marks by studentId+subjectId
        var internalLookup = internalMarks
            .ToDictionary(m => (m.StudentId, m.SubjectId), m => m);

        // Collect all student IDs
        var allStudentIds = externalByStudent.Keys.ToList();

        // Load existing results for update (or we'll create new)
        var existingResults = await _db.StudentResults
            .Where(r => r.SemesterId == request.SemesterId)
            .ToListAsync(cancellationToken);

        var existingResultMap = existingResults
            .ToDictionary(r => (r.StudentId, r.SubjectId), r => r);

        // Load all previous semester results for CGPA calculation
        var previousResults = await _db.StudentResults
            .Where(r => r.SemesterId != request.SemesterId && r.IsPublished && allStudentIds.Contains(r.StudentId))
            .ToListAsync(cancellationToken);

        var publishedAt = DateTime.UtcNow;
        int publishedCount = 0;

        // Process per student
        foreach (var studentId in allStudentIds)
        {
            var studentExternals = externalByStudent[studentId];
            var subjectResults = new List<(decimal gradePoints, int credits)>();

            foreach (var extMark in studentExternals)
            {
                var schedule = scheduleMap[extMark.ExamScheduleId];
                internalLookup.TryGetValue((studentId, schedule.SubjectId), out var intMark);

                var internalMarksValue = intMark?.Marks ?? 0m;
                var externalMarksValue = extMark.IsAbsent ? 0m : extMark.Marks;
                var totalMarks = internalMarksValue + externalMarksValue;
                var maxMarks = (intMark?.MaxMarks ?? 50m) + extMark.MaxMarks;

                var (gradeLetter, gradePoints) = calculator.GetGrade(totalMarks, maxMarks, gradeRules);

                var status = totalMarks >= schedule.PassingMarks ? ResultStatus.Pass : ResultStatus.Fail;

                // Credits default to 3 (would come from curriculum in a full system)
                const int credits = 3;

                subjectResults.Add((gradePoints, credits));

                if (existingResultMap.TryGetValue((studentId, schedule.SubjectId), out var existingResult))
                {
                    existingResult.InternalMarks = internalMarksValue;
                    existingResult.ExternalMarks = externalMarksValue;
                    existingResult.TotalMarks = totalMarks;
                    existingResult.MaxMarks = maxMarks;
                    existingResult.GradeLetter = gradeLetter;
                    existingResult.GradePoints = gradePoints;
                    existingResult.Credits = credits;
                    existingResult.Status = status;
                    existingResult.IsPublished = true;
                    existingResult.PublishedAt = publishedAt;
                }
                else
                {
                    var newResult = new StudentResult
                    {
                        TenantId = request.TenantId,
                        StudentId = studentId,
                        SemesterId = request.SemesterId,
                        SubjectId = schedule.SubjectId,
                        SubjectName = schedule.SubjectName,
                        InternalMarks = internalMarksValue,
                        ExternalMarks = externalMarksValue,
                        TotalMarks = totalMarks,
                        MaxMarks = maxMarks,
                        GradeLetter = gradeLetter,
                        GradePoints = gradePoints,
                        Credits = credits,
                        Status = status,
                        IsPublished = true,
                        PublishedAt = publishedAt
                    };
                    _db.StudentResults.Add(newResult);
                    existingResultMap[(studentId, schedule.SubjectId)] = newResult;
                }
            }

            // Calculate semester GPA
            var semesterGpa = calculator.CalculateGpa(subjectResults);
            var semesterCredits = subjectResults.Sum(s => s.credits);

            // Calculate CGPA using previous published semesters
            var previousSemesterGroups = previousResults
                .Where(r => r.StudentId == studentId && r.GPA.HasValue)
                .GroupBy(r => r.SemesterId)
                .Select(g => (gpa: g.First().GPA!.Value, semesterCredits: g.Sum(r => r.Credits)))
                .ToList();

            previousSemesterGroups.Add((semesterGpa, semesterCredits));
            var cgpa = calculator.CalculateCgpa(previousSemesterGroups);

            // Update GPA/CGPA on all results for this student/semester
            foreach (var subjectEntry in studentExternals)
            {
                var schedule = scheduleMap[subjectEntry.ExamScheduleId];
                if (existingResultMap.TryGetValue((studentId, schedule.SubjectId), out var res))
                {
                    res.GPA = semesterGpa;
                    res.CGPA = cgpa;
                }
            }

            publishedCount++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Publish domain event
        await _mediator.Publish(new ExamResultPublishedEvent(request.TenantId, request.SemesterId, publishedCount), cancellationToken);

        return Result<int>.Success(publishedCount);
    }
}
