using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Commands;

public record UploadContentCommand(
    Guid TenantId,
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string? Description,
    ContentType ContentType,
    byte[]? FileBytes,
    string? FileName,
    string? ExternalUrl,
    int OrderIndex,
    Guid UploadedBy) : IRequest<Result<Guid>>;

public class UploadContentHandler : IRequestHandler<UploadContentCommand, Result<Guid>>
{
    private readonly ILmsDbContext _db;
    private readonly IAzureBlobService _blob;

    public UploadContentHandler(ILmsDbContext db, IAzureBlobService blob)
    {
        _db = db;
        _blob = blob;
    }

    public async Task<Result<Guid>> Handle(UploadContentCommand cmd, CancellationToken ct)
    {
        string? blobUrl = null;

        if (cmd.ContentType != ContentType.Link)
        {
            if (cmd.FileBytes is null || string.IsNullOrWhiteSpace(cmd.FileName))
                return Result.Failure<Guid>("File bytes and file name are required for non-link content.");

            var blobName = $"{cmd.TenantId}/{cmd.SubjectId}/{Guid.NewGuid()}/{cmd.FileName}";
            blobUrl = await _blob.UploadAsync("lms-content", blobName, cmd.FileBytes, GetMimeType(cmd.FileName), ct);
        }

        var content = new CourseContent
        {
            TenantId  = cmd.TenantId,
            SubjectId  = cmd.SubjectId,
            BatchId    = cmd.BatchId,
            Title      = cmd.Title,
            Description = cmd.Description,
            ContentType = cmd.ContentType,
            BlobUrl    = blobUrl,
            ExternalUrl = cmd.ContentType == ContentType.Link ? cmd.ExternalUrl : null,
            OrderIndex = cmd.OrderIndex,
            IsVisible  = true,
            UploadedBy = cmd.UploadedBy
        };

        _db.CourseContents.Add(content);

        // Bump TotalContentCount for all existing progress rows for this subject+batch
        await _db.StudentProgresses
            .Where(p => p.TenantId == cmd.TenantId && p.SubjectId == cmd.SubjectId && p.BatchId == cmd.BatchId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.TotalContentCount, p => p.TotalContentCount + 1), ct);

        await _db.SaveChangesAsync(ct);
        return Result.Success(content.Id);
    }

    private static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf"  => "application/pdf",
            ".mp4"  => "video/mp4",
            ".webm" => "video/webm",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc"  => "application/msword",
            _       => "application/octet-stream"
        };
    }
}
