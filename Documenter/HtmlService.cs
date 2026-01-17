using System.Text;
using Markdig; // Requires 'Markdig' NuGet package

namespace Documenter
{
    public class HtmlService
    {
        private StringBuilder _content = new StringBuilder();

        public HtmlService()
        {
            // Add CSS Styles (Github Theme)
            _content.Append(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { font-family: 'Segoe UI', sans-serif; line-height: 1.6; color: #333; max-width: 900px; margin: 0 auto; padding: 20px; }
                        h1 { color: #2c3e50; border-bottom: 2px solid #eee; padding-bottom: 10px; }
                        h2 { color: #0366d6; margin-top: 30px; border-bottom: 1px solid #eaecef; padding-bottom: 5px; }
                        h3 { color: #24292e; margin-top: 20px; }
                        table { border-collapse: collapse; width: 100%; margin: 15px 0; font-size: 14px; }
                        th, td { border: 1px solid #dfe2e5; padding: 8px 12px; text-align: left; }
                        th { background-color: #f6f8fa; font-weight: 600; }
                        tr:nth-child(even) { background-color: #f8f8f8; }
                        code { background: #f0f0f0; padding: 2px 5px; border-radius: 3px; font-family: Consolas, monospace; color: #d73a49; }
                        .section-break { page-break-after: always; display: block; height: 1px; margin: 30px 0; }
                        .readme-box { background: #fcfcfc; border: 1px solid #ddd; padding: 20px; border-radius: 5px; margin-bottom: 40px; }
                    </style>
                </head>
                <body>
            ");
        }

        public void AddMarkdown(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string html = Markdown.ToHtml(markdown, pipeline);
            _content.Append($"<div class='doc-section'>{html}</div>");
            _content.Append("<div class='section-break'></div>"); // Force new page in PDF
        }

        public void AddReadme(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string html = Markdown.ToHtml(markdown, pipeline);
            // Insert Readme at the VERY TOP of the body (using a placeholder technique or just prepending)
            // For simplicity, we add it with a special class
            _content.Insert(_content.ToString().IndexOf("<body>") + 6,
                $"<div class='readme-box'><h1>📖 Project Guide (README)</h1>{html}</div><div class='section-break'></div>");
        }

        public string GetHtml()
        {
            _content.Append("</body></html>");
            return _content.ToString();
        }
    }
}