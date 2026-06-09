using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.SIS.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.SIS.Application.Commands;

public class GenerateIdCardHandler : IRequestHandler<GenerateIdCardCommand, Result<byte[]>>
{
    private readonly ISisDbContext _db;
    private readonly IPdfService _pdf;

    public GenerateIdCardHandler(ISisDbContext db, IPdfService pdf)
    {
        _db = db;
        _pdf = pdf;
    }

    public async Task<Result<byte[]>> Handle(GenerateIdCardCommand request, CancellationToken cancellationToken)
    {
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);
        if (student is null)
            return Result.Failure<byte[]>("Student not found.");

        var fullName = $"{student.FirstName} {student.LastName}".Trim();
        var html = $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'><style>
body {{ font-family: Arial, sans-serif; margin: 0; }}
.card {{ width: 320px; height: 200px; border: 2px solid #003366; border-radius: 10px;
         padding: 16px; background: linear-gradient(135deg, #003366, #0066cc); color: white; }}
.name {{ font-size: 18px; font-weight: bold; margin-top: 8px; }}
.detail {{ font-size: 13px; margin-top: 4px; }}
.label {{ color: #aad4ff; font-size: 11px; }}
</style></head><body>
<div class='card'>
  <div style='font-size:14px; font-weight:bold; letter-spacing:1px;'>STUDENT IDENTITY CARD</div>
  <div class='name'>{fullName}</div>
  <div class='detail'><span class='label'>Student No: </span>{student.StudentNumber}</div>
  <div class='detail'><span class='label'>Programme: </span>{student.ProgramName}</div>
  <div class='detail'><span class='label'>Academic Year: </span>{student.AcademicYear}</div>
  <div class='detail'><span class='label'>Email: </span>{student.Email}</div>
</div>
</body></html>";

        var bytes = await _pdf.GeneratePdfAsync(html, cancellationToken);
        return Result.Success(bytes);
    }
}
