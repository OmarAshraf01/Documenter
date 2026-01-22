using System.Text;
using Markdig;

namespace Documenter
{
    public class HtmlService
    {
        // Store sections separately to avoid search/replace crashes
        private string _treeSection = "";
        private string _diagramSection = "";
        private string _schemaSection = "";
        private string _readmeSection = "";
        private StringBuilder _codeAnalysisSection = new StringBuilder();

        public HtmlService()
        {
            _codeAnalysisSection.Append("<hr/><h1>📘 Code Analysis</h1>");
        }

        public void InjectProjectStructure(string treeStructure)
        {
            _treeSection = $@"
                <div class='doc-section'>
                    <h1>📂 Project Folder Structure</h1>
                    <div class='tree-box'>{treeStructure}</div>
                </div><div class='section-break'></div>";
        }

        public void InjectDatabaseSchema(string rawAiOutput)
        {
            if (string.IsNullOrWhiteSpace(rawAiOutput) || rawAiOutput.Length < 10) return;

            _schemaSection = $@"
                <div class='doc-section'>
                    <div class='diagram-box'>
                        <h2>🗄️ Suggested Database Schema</h2>
                        <div class='mermaid'>{rawAiOutput}</div>
                    </div>
                </div><div class='section-break'></div>";
        }

        public void InjectDiagram(string rawAiOutput)
        {
            _diagramSection = $@"
                <div class='doc-section'>
                    <div class='diagram-box'>
                        <h2>🏗️ Architecture Diagram</h2>
                        <div class='mermaid'>{rawAiOutput}</div>
                    </div>
                </div><div class='section-break'></div>";
        }

        public void InjectReadme(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseGridTables().Build();
            string html = Markdown.ToHtml(markdown, pipeline);
            _readmeSection = $"<div class='doc-section'>{html}</div><div class='section-break'></div>";
        }

        public void AddMarkdown(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseGridTables().Build();
            string html = Markdown.ToHtml(markdown, pipeline);
            _codeAnalysisSection.Append($"<div class='doc-section'>{html}</div><div class='section-break'></div>");
        }

        public string GetHtml()
        {
            var sb = new StringBuilder();
            sb.Append(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <script src='[https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js](https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js)'></script>
                    <script>
                        mermaid.initialize({
                            startOnLoad: true,
                            theme: 'neutral',
                            securityLevel: 'loose',
                            maxEdges: 500,
                            flowchart: { useMaxWidth: false, htmlLabels: true }
                        });
                    </script>
                    <style>
                        body { font-family: 'Segoe UI', Helvetica, sans-serif; line-height: 1.4; color: #24292e; max-width: 900px; margin: 0 auto; padding: 20px; font-size: 11px; }
                        h1 { border-bottom: 2px solid #eaecef; padding-bottom: 5px; font-size: 22px; margin-top: 30px; color: #0366d6; }
                        h2 { border-bottom: 1px solid #eaecef; padding-bottom: 5px; font-size: 18px; margin-top: 25px; }
                        .tree-box { background-color: #f1f8ff; border: 1px solid #c8e1ff; border-radius: 5px; padding: 15px; font-family: 'Consolas', monospace; font-size: 11px; white-space: pre; overflow-x: auto; display: block; }
                        .diagram-box { text-align: center; margin: 20px 0; padding: 10px; border: 1px dashed #ccc; border-radius: 5px; page-break-inside: avoid; }
                        .mermaid { display: flex; justify-content: center; }
                        .doc-section { margin-bottom: 30px; }
                        .section-break { page-break-after: always; display: block; height: 1px; margin: 20px 0; }
                        table { border-collapse: collapse; width: 100%; margin: 15px 0; font-size: 11px; }
                        th, td { border: 1px solid #dfe2e5; padding: 6px 12px; text-align: left; }
                        th { background-color: #f6f8fa; font-weight: 700; }
                        tr:nth-child(2n) { background-color: #fcfcfc; }
                    </style>
                </head>
                <body>");

            // Append sections in logical order
            sb.Append(_treeSection);
            sb.Append(_diagramSection);
            sb.Append(_schemaSection);
            sb.Append(_readmeSection);
            sb.Append(_codeAnalysisSection);

            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}