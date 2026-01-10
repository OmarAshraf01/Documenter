using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Documenter
{
    public class PdfReport
    {
        public static void Generate(string filePath, string content)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.SegoeUI));

                    page.Header()
                        .Text("AI Generated Documentation")
                        .Bold().FontSize(20).FontColor(Colors.Blue.Darken2).AlignCenter();

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        foreach (var line in content.Split('\n'))
                        {
                            var clean = line.Trim();
                            if (clean.StartsWith("## 📂")) // New File Header
                            {
                                col.Item().PageBreak();
                                col.Item().Text(clean.Replace("#", "").Trim()).FontSize(16).Bold().FontColor(Colors.Red.Darken2);
                                col.Item().LineHorizontal(1);
                            }
                            else if (clean.StartsWith("###")) // Section Header
                            {
                                col.Item().PaddingTop(10).Text(clean.Replace("#", "").Trim()).FontSize(12).Bold();
                            }
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(clean)) col.Item().Text(clean);
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            })
            .GeneratePdf(filePath);
        }
    }
}