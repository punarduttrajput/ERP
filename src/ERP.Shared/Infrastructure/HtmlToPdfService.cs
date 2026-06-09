using DinkToPdf;
using DinkToPdf.Contracts;
using ERP.Shared.Application.Abstractions;

namespace ERP.Shared.Infrastructure;

public class HtmlToPdfService : IPdfService
{
    private readonly IConverter _converter;

    public HtmlToPdfService(IConverter converter)
    {
        _converter = converter;
    }

    public Task<byte[]> GeneratePdfAsync(string html, CancellationToken cancellationToken = default)
    {
        var doc = new HtmlToPdfDocument
        {
            GlobalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4
            },
            Objects =
            {
                new ObjectSettings
                {
                    PagesCount = true,
                    HtmlContent = html,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
        };

        var bytes = _converter.Convert(doc);
        return Task.FromResult(bytes);
    }
}
