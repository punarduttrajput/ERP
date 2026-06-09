namespace ERP.SIS.API.Dtos;

public record UploadDocumentRequest(
    string DocumentType,
    string OriginalFileName,
    string ContentType,
    string FileContentBase64
);
