namespace ERP.Shared.Application.Abstractions;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendTemplatedAsync(string to, string templateName, object templateData, CancellationToken cancellationToken = default);
}
