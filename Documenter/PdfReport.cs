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
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                    // --- HEADER ---
                    page.Header()
                        .Text("PROJECT DOCUMENTATION")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken2).AlignCenter();

                    // --- CONTENT LOOP ---
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                    {
                        foreach (var line in content.Split('\n'))
                        {
                            var cleanLine = line.Trim();

                            // 1. File Headers (## 📄 FileName)
                            if (cleanLine.StartsWith("##"))
                            {
                                col.Item().PaddingTop(20).Text(cleanLine.Replace("#", "").Trim())
                                   .FontSize(18).Bold().FontColor(Colors.Teal.Medium);
                                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                            }
                            // 2. Sub-Headers (### ⚡ Key Features)
                            else if (cleanLine.StartsWith("###"))
                            {
                                col.Item().PaddingTop(10).Text(cleanLine.Replace("#", "").Trim())
                                   .FontSize(14).SemiBold().FontColor(Colors.Orange.Darken2);
                            }
                            // 3. Bullet Points (*)
                            else if (cleanLine.StartsWith("*") || cleanLine.StartsWith("-"))
                            {
                                col.Item().PaddingLeft(10).Text("• " + cleanLine.TrimStart('*', '-', ' '))
                                   .FontSize(11);
                            }
                            // 4. Code Blocks / Diagrams (Skip raw mermaid syntax in text, just show it's there)
                            else if (cleanLine.StartsWith("```"))
                            {
                                // Skip the ``` markers visually
                            }
                            // 5. Normal Text
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(cleanLine))
                                    col.Item().Text(cleanLine);
                            }
                        }
                    });

                    // --- FOOTER ---
                    page.Footer()
                        .AlignCenter()
                        .Text(x => {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                });
            })
            .GeneratePdf(filePath);
        }
    }
}