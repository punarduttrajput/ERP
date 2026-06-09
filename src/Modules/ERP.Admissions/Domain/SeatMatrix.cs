using ERP.Shared.Domain;

namespace ERP.Admissions.Domain;

public class SeatMatrix : TenantEntity
{
    public Guid ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public string Category { get; set; } = "General";
    public int TotalSeats { get; set; }
    public int FilledSeats { get; set; }
    public int AvailableSeats => TotalSeats - FilledSeats;
}
