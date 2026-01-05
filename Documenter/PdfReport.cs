using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AutoDocGui
{
    public class PdfReport
    {
        public static void Generate(string filePath, string content)
        {
            QuestPDF.Settings.License = LicenseType.Community; // Free license

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Text("Universal Code Documentation")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                    {
                        foreach (var line in content.Split('\n'))
                        {
                            if (line.Trim().StartsWith("##"))
                                x.Item().Text(line.Replace("#", "")).FontSize(16).Bold();
                            else if (line.Trim().StartsWith("**Language**"))
                                x.Item().Text(line).FontSize(12).Italic().FontColor(Colors.Grey.Darken2);
                            else
                                x.Item().Text(line);
                        }
                    });

                    page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); });
                });
            })
            .GeneratePdf(filePath);
        }
    }
}