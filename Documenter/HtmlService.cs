using System.Text;
using System.Text.RegularExpressions;
using Markdig;

namespace Documenter
{
    public class HtmlService
    {
        private StringBuilder _content = new StringBuilder();

        // --- CRITICAL FIX: These must NOT be empty strings ("") ---
        private const string TreePlaceholder = "";
        private const string ReadmePlaceholder = "";
        private const string DiagramPlaceholder = "";
        private const string SchemaPlaceholder = "";

        private const string SectionBreak = "<div class='section-break'></div>";

        public HtmlService()
        {
            // Initialize HTML Structure
            _content.Append($@"
                <!DOCTYPE html>
                <html>
                <head>
                    <script src='https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js'></script>
                    <script>
                        mermaid.initialize({{
                            startOnLoad: true,
                            theme: 'neutral',
                            securityLevel: 'loose',
                            flowchart: {{ useMaxWidth: false, htmlLabels: true }}
                        }});
                    </script>
                    <style>
                        body {{ font-family: 'Segoe UI', Helvetica, sans-serif; line-height: 1.4; color: #24292e; max-width: 900px; margin: 0 auto; padding: 20px; font-size: 11px; }}
                        h1 {{ border-bottom: 2px solid #eaecef; padding-bottom: 5px; font-size: 22px; margin-top: 30px; color: #0366d6; }}
                        h2 {{ border-bottom: 1px solid #eaecef; padding-bottom: 5px; font-size: 18px; margin-top: 25px; }}
                        .tree-box {{ background-color: #f1f8ff; border: 1px solid #c8e1ff; border-radius: 5px; padding: 15px; font-family: 'Consolas', monospace; font-size: 11px; white-space: pre; overflow-x: auto; display: block; }}
                        .diagram-box {{ text-align: center; margin: 20px 0; padding: 10px; border: 1px dashed #ccc; border-radius: 5px; page-break-inside: avoid; }}
                        .mermaid {{ display: flex; justify-content: center; }}
                        .doc-section {{ margin-bottom: 30px; }}
                        .section-break {{ page-break-after: always; display: block; height: 1px; margin: 20px 0; }}
                        table {{ border-collapse: collapse; width: 100%; margin: 15px 0; font-size: 11px; }}
                        th, td {{ border: 1px solid #dfe2e5; padding: 6px 12px; text-align: left; }}
                        th {{ background-color: #f6f8fa; font-weight: 700; }}
                        tr:nth-child(2n) {{ background-color: #fcfcfc; }}
                    </style>
                </head>
                <body>
                    {TreePlaceholder}
                    {ReadmePlaceholder}
                    {DiagramPlaceholder}
                    {SchemaPlaceholder}
                    <hr/>
                    <h1>📘 Code Analysis</h1>
            ");
        }

        public void InjectProjectStructure(string treeStructure)
        {
            string html = $@"
                <h1>📂 Project Structure</h1>
                <div class='tree-box'>{treeStructure}</div>
                {SectionBreak}";
            SafeReplace(TreePlaceholder, html);
        }

        public void InjectDatabaseSchema(string rawAiOutput)
        {
            string cleanCode = CleanMermaid(rawAiOutput);
            if (string.IsNullOrWhiteSpace(cleanCode) || cleanCode.Contains("Error")) return;

            string html = $@"
                <div class='diagram-box'>
                    <h2>🗄️ Database Schema (ERD)</h2>
                    <div class='mermaid'>
                        {cleanCode}
                    </div>
                </div>
                {SectionBreak}";
            SafeReplace(SchemaPlaceholder, html);
        }

        public void InjectDiagram(string rawAiOutput)
        {
            string cleanCode = CleanMermaid(rawAiOutput);
            string html = $@"
                <div class='diagram-box'>
                    <h2>🏗️ Architecture Diagram</h2>
                    <div class='mermaid'>
                        {cleanCode}
                    </div>
                </div>
                {SectionBreak}";
            SafeReplace(DiagramPlaceholder, html);
        }

        public void InjectReadme(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string html = Markdown.ToHtml(markdown, pipeline);
            SafeReplace(ReadmePlaceholder, $"<div class='doc-section'>{html}</div>{SectionBreak}");
        }

        public void AddMarkdown(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseGridTables().UsePipeTables().Build();
            string html = Markdown.ToHtml(markdown, pipeline);
            _content.Append($"<div class='doc-section'>{html}</div>{SectionBreak}");
        }

        private void SafeReplace(string placeholder, string newValue)
        {
            // Safety Check: If placeholder is somehow empty, do nothing to prevent crash
            if (string.IsNullOrEmpty(placeholder)) return;
            if (_content.ToString().Contains(placeholder))
                _content.Replace(placeholder, newValue ?? "");
        }

        private string CleanMermaid(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "graph TD;\nError[Empty Data];";
            var match = Regex.Match(raw, @"```mermaid\s*([\s\S]*?)\s*```");
            if (match.Success) return match.Groups[1].Value.Trim();
            return raw.Replace("```mermaid", "").Replace("```", "").Replace("mermaid", "").Trim();
        }

        public string GetHtml()
        {
            // Cleanup unused placeholders
            SafeReplace(TreePlaceholder, "");
            SafeReplace(ReadmePlaceholder, "");
            SafeReplace(DiagramPlaceholder, "");
            SafeReplace(SchemaPlaceholder, "");
            return _content.ToString() + "</body></html>";
        }
    }
}