using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.SIS.Domain;
using ERP.SIS.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.SIS.Application.Commands;

public class GenerateCertificateHandler : IRequestHandler<GenerateCertificateCommand, Result<byte[]>>
{
    private readonly ISisDbContext _db;
    private readonly IPdfService _pdf;

    public GenerateCertificateHandler(ISisDbContext db, IPdfService pdf)
    {
        _db = db;
        _pdf = pdf;
    }

    public async Task<Result<byte[]>> Handle(GenerateCertificateCommand request, CancellationToken cancellationToken)
    {
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);
        if (student is null)
            return Result.Failure<byte[]>("Student not found.");

        var fullName = $"{student.FirstName} {student.LastName}".Trim();
        var html = request.CertificateType switch
        {
            CertificateType.Bonafide => BuildBonafideHtml(fullName, student.ProgramName, student.AcademicYear),
            CertificateType.Character => BuildCharacterHtml(fullName),
            CertificateType.Provisional => BuildProvisionalHtml(fullName),
            _ => throw new ArgumentOutOfRangeException(nameof(request.CertificateType))
        };

        var bytes = await _pdf.GeneratePdfAsync(html, cancellationToken);
        return Result.Success(bytes);
    }

    private static string BuildBonafideHtml(string name, string program, int year) => $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'><style>
body {{ font-family: Arial, sans-serif; margin: 60px; }}
h1 {{ text-align: center; text-decoration: underline; }}
p {{ font-size: 16px; line-height: 1.8; }}
</style></head><body>
<h1>Bonafide Certificate</h1>
<p>This is to certify that <strong>{name}</strong>, student of <strong>{program}</strong>,
is a bonafide student of this institution for the academic year <strong>{year}</strong>.</p>
<p>This certificate is issued for academic purposes only.</p>
</body></html>";

    private static string BuildCharacterHtml(string name) => $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'><style>
body {{ font-family: Arial, sans-serif; margin: 60px; }}
h1 {{ text-align: center; text-decoration: underline; }}
p {{ font-size: 16px; line-height: 1.8; }}
</style></head><body>
<h1>Character Certificate</h1>
<p>This is to certify that <strong>{name}</strong> has been a student of good character
during their tenure at this institution.</p>
<p>We wish them success in all future endeavours.</p>
</body></html>";

    private static string BuildProvisionalHtml(string name) => $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'><style>
body {{ font-family: Arial, sans-serif; margin: 60px; }}
h1 {{ text-align: center; text-decoration: underline; }}
p {{ font-size: 16px; line-height: 1.8; }}
</style></head><body>
<h1>Provisional Certificate</h1>
<p>This provisional certificate is issued to <strong>{name}</strong> pending issuance
of the final degree certificate.</p>
<p>This certificate is valid until the original degree certificate is issued.</p>
</body></html>";
}
