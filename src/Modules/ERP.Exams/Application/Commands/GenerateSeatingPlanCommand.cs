using ERP.Exams.Domain;
using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Common;
using ERP.Shared.Application.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Application.Commands;

public record StudentSeatInfo(Guid StudentId, string RollNumber);

public record GenerateSeatingPlanCommand(
    Guid TenantId,
    Guid ExamScheduleId,
    string SeatingOrder,   // "Random" | "RollNumber"
    IReadOnlyList<StudentSeatInfo> Students) : IRequest<Result<int>>;

public class GenerateSeatingPlanHandler : IRequestHandler<GenerateSeatingPlanCommand, Result<int>>
{
    private readonly IExamsDbContext _db;
    private readonly IFeeService _feeService;

    public GenerateSeatingPlanHandler(IExamsDbContext db, IFeeService feeService)
    {
        _db = db;
        _feeService = feeService;
    }

    public async Task<Result<int>> Handle(GenerateSeatingPlanCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _db.ExamSchedules
            .FirstOrDefaultAsync(s => s.Id == request.ExamScheduleId, cancellationToken);

        if (schedule is null)
            return Result<int>.Failure("Exam schedule not found.");

        // Delete existing allocations for this schedule
        var existing = await _db.SeatAllocations
            .Where(s => s.ExamScheduleId == request.ExamScheduleId)
            .ToListAsync(cancellationToken);

        foreach (var alloc in existing)
            _db.SeatAllocations.Remove(alloc);

        // Order students
        var ordered = request.SeatingOrder == "RollNumber"
            ? request.Students.OrderBy(s => s.RollNumber).ToList()
            : Shuffle(request.Students.ToList());

        // Short code for seat number prefix — last 3 digits of schedule Id
        var shortCode = request.ExamScheduleId.ToString("N")[^3..].ToUpper();

        // Check internal marks entered per student for this semester
        var internalMarksStudents = await _db.InternalMarks
            .Where(m => m.SemesterId == schedule.SemesterId)
            .Select(m => m.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var allocations = new List<SeatAllocation>();
        int seqNum = 1;

        foreach (var student in ordered)
        {
            int row = ((seqNum - 1) / 10) + 1;
            int col = ((seqNum - 1) % 10) + 1;
            var seatNumber = $"H{shortCode}-{row:D2}-{col:D2}";

            bool isEligible = true;
            string? ineligibilityReason = null;

            // Fee dues check
            var balance = await _feeService.GetOutstandingBalanceAsync(student.StudentId, cancellationToken);
            if (balance > 0)
            {
                isEligible = false;
                ineligibilityReason = "Outstanding fee dues.";
            }

            // Internal marks check
            if (isEligible && !internalMarksStudents.Contains(student.StudentId))
            {
                isEligible = false;
                ineligibilityReason = "Internal marks pending.";
            }

            allocations.Add(new SeatAllocation
            {
                TenantId = request.TenantId,
                ExamScheduleId = request.ExamScheduleId,
                StudentId = student.StudentId,
                RollNumber = student.RollNumber,
                SeatNumber = seatNumber,
                HallTicketGenerated = false,
                IsEligible = isEligible,
                IneligibilityReason = ineligibilityReason
            });

            seqNum++;
        }

        foreach (var alloc in allocations)
            _db.SeatAllocations.Add(alloc);

        await _db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(allocations.Count);
    }

    private static List<T> Shuffle<T>(List<T> list)
    {
        var rng = new Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}
