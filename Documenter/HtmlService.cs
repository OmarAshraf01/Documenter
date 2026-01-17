using System.Text;
using Markdig;

namespace Documenter
{
    public class HtmlService
    {
        private StringBuilder _content = new StringBuilder();

        public HtmlService()
        {
            _content.Append(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <script src='https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js'></script>
                    <script>mermaid.initialize({startOnLoad:true});</script>

                    <style>
                        body { font-family: 'Segoe UI', sans-serif; line-height: 1.6; color: #333; max-width: 950px; margin: 0 auto; padding: 40px; }
                        
                        /* HEADERS */
                        h1 { color: #2c3e50; border-bottom: 2px solid #eee; padding-bottom: 10px; margin-top: 40px; }
                        h2 { color: #0366d6; margin-top: 30px; border-bottom: 1px solid #eaecef; padding-bottom: 5px; }
                        
                        /* TABLES (Fixed Alignment) */
                        table { border-collapse: collapse; width: 100%; margin: 20px 0; font-size: 14px; table-layout: fixed; }
                        th, td { border: 1px solid #dfe2e5; padding: 10px 15px; text-align: left; word-wrap: break-word; }
                        th { background-color: #f6f8fa; font-weight: 700; color: #24292e; }
                        tr:nth-child(even) { background-color: #f8f8f8; }

                        /* TREE VIEW */
                        .tree-box { background: #1e1e1e; color: #d4d4d4; padding: 20px; border-radius: 8px; font-family: 'Consolas', monospace; white-space: pre; overflow-x: auto; margin-bottom: 40px; }
                        
                        /* README & DIAGRAMS */
                        .readme-box { background: #fcfcfc; border: 1px solid #e1e4e8; padding: 30px; border-radius: 6px; margin-bottom: 40px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
                        .diagram-box { text-align: center; margin: 30px 0; padding: 20px; border: 1px dashed #ccc; border-radius: 5px; }
                        
                        .section-break { page-break-after: always; display: block; height: 1px; margin: 30px 0; }
                    </style>
                </head>
                <body>
            ");
        }

        public void AddProjectStructure(string treeStructure)
        {
            _content.Insert(_content.ToString().IndexOf("<body>") + 6,
                $"<div class='tree-box'><h3>📂 File Structure</h3>{treeStructure}</div><div class='section-break'></div>");
        }

        public void AddDiagram(string mermaidCode)
        {
            // Clean up the code block markdown if the AI adds it
            string cleanCode = mermaidCode.Replace("```mermaid", "").Replace("```", "").Trim();

            string html = $@"
                <div class='diagram-box'>
                    <h3>🏗️ Architecture Diagram</h3>
                    <div class='mermaid'>
                        {cleanCode}
                    </div>
                </div>
                <div class='section-break'></div>";

            // Insert after Body tag (or after Tree View)
            _content.Insert(_content.ToString().IndexOf("<body>") + 6, html);
        }

        public void AddMarkdown(string markdown)
        {
            // Use Advanced Extensions for Tables
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseGridTables().UsePipeTables().Build();
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