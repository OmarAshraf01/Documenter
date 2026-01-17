using System.Text;
using Markdig;

namespace Documenter
{
    public class HtmlService
    {
        private StringBuilder _content = new StringBuilder();

        public HtmlService()
        {
            // CSS: Added styling for the Project Tree (.tree-view)
            _content.Append(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body { font-family: 'Segoe UI', sans-serif; line-height: 1.6; color: #333; max-width: 900px; margin: 0 auto; padding: 40px; }
                        h1 { color: #2c3e50; border-bottom: 2px solid #eee; padding-bottom: 10px; }
                        h2 { color: #0366d6; margin-top: 30px; border-bottom: 1px solid #eaecef; padding-bottom: 5px; }
                        
                        /* Tree View Styling */
                        .tree-box { background: #1e1e1e; color: #d4d4d4; padding: 20px; border-radius: 8px; font-family: 'Consolas', monospace; white-space: pre; overflow-x: auto; margin-bottom: 40px; }
                        .tree-title { font-size: 1.2em; font-weight: bold; color: #4ec9b0; margin-bottom: 10px; display: block; }

                        /* Readme Styling */
                        .readme-box { background: #fcfcfc; border: 1px solid #e1e4e8; padding: 30px; border-radius: 6px; margin-bottom: 40px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
                        
                        table { border-collapse: collapse; width: 100%; margin: 15px 0; font-size: 14px; }
                        th, td { border: 1px solid #dfe2e5; padding: 8px 12px; text-align: left; }
                        th { background-color: #f6f8fa; font-weight: 600; }
                        tr:nth-child(even) { background-color: #f8f8f8; }
                        .section-break { page-break-after: always; display: block; height: 1px; margin: 30px 0; }
                    </style>
                </head>
                <body>
            ");
        }

        public void AddProjectStructure(string treeStructure)
        {
            // Insert at the very TOP (after body)
            _content.Insert(_content.ToString().IndexOf("<body>") + 6,
                $"<div class='tree-box'><span class='tree-title'>📂 Project Structure</span>{treeStructure}</div><div class='section-break'></div>");
        }

        public void AddReadme(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string html = Markdown.ToHtml(markdown, pipeline);
            // Insert after the Tree View (calculated by finding the section break)
            _content.Append($"<div class='readme-box'><h1>📖 User Guide (README)</h1>{html}</div><div class='section-break'></div>");
        }

        public void AddMarkdown(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string html = Markdown.ToHtml(markdown, pipeline);
            _content.Append($"<div class='doc-section'>{html}</div><div class='section-break'></div>");
        }

        public string GetHtml()
        {
            _content.Append("</body></html>");
            return _content.ToString();
        }
    }
}