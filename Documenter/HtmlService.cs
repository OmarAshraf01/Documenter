using System.Text;
using Markdig;

namespace Documenter
{
    public class HtmlService
    {
        private StringBuilder _content = new StringBuilder();
        private const string SectionBreak = "<div class='section-break'></div>";

        public HtmlService()
        {
            _content.Append(@"
                <!DOCTYPE html>
                <html>
                <head>
                    <script src='https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js'></script>
                    <script>mermaid.initialize({startOnLoad:true});</script>

                    <style>
                        /* GLOBAL FONTS - MADE LARGER */
                        body { font-family: 'Segoe UI', Helvetica, sans-serif; line-height: 1.6; color: #24292e; max-width: 1000px; margin: 0 auto; padding: 40px; font-size: 16px; }
                        
                        h1 { border-bottom: 1px solid #eaecef; padding-bottom: .3em; font-size: 2.25em; margin-top: 40px; }
                        h2 { border-bottom: 1px solid #eaecef; padding-bottom: .3em; font-size: 1.75em; margin-top: 30px; }
                        h3 { font-size: 1.5em; margin-top: 24px; }

                        /* TREE VIEW - FIXED ALIGNMENT */
                        .tree-box { 
                            background-color: #f6f8fa; 
                            border: 1px solid #e1e4e8; 
                            border-radius: 6px; 
                            padding: 16px; 
                            margin-bottom: 30px; 
                            font-family: 'Consolas', 'Courier New', monospace; 
                            font-size: 14px; 
                            line-height: 1.45; 
                            overflow: auto; 
                            white-space: pre; /* CRITICAL for tree alignment */
                        }

                        /* TABLES */
                        table { border-collapse: collapse; width: 100%; margin: 20px 0; display: table; }
                        th, td { border: 1px solid #dfe2e5; padding: 10px 16px; text-align: left; }
                        th { background-color: #f6f8fa; font-weight: 600; }
                        tr:nth-child(2n) { background-color: #fcfcfc; }

                        /* DIAGRAMS */
                        .diagram-box { text-align: center; margin: 30px 0; padding: 20px; border: 1px solid #eee; border-radius: 6px; }
                        
                        /* BREAKS */
                        .section-break { page-break-after: always; display: block; height: 1px; margin: 40px 0; }
                        
                        /* CODE BLOCKS */
                        code { padding: 0.2em 0.4em; margin: 0; font-size: 85%; background-color: rgba(27,31,35,0.05); border-radius: 3px; font-family: 'Consolas', monospace; }
                        pre { background-color: #f6f8fa; padding: 16px; border-radius: 6px; overflow: auto; }
                    </style>
                </head>
                <body>
            ");
        }

        public void AddProjectStructure(string treeStructure)
        {
            // Using <pre> tag ensures the tree lines don't break
            string html = $@"
                <h1>📂 Project Structure</h1>
                <div class='tree-box'>
<pre>{treeStructure}</pre>
                </div>
                {SectionBreak}";

            _content.Insert(_content.ToString().IndexOf("<body>") + 6, html);
        }

        public void AddDiagram(string mermaidCode)
        {
            string cleanCode = mermaidCode.Replace("```mermaid", "").Replace("```", "").Trim();
            string html = $@"
                <div class='diagram-box'>
                    <h2>🏗️ Architecture Diagram</h2>
                    <div class='mermaid'>
                        {cleanCode}
                    </div>
                </div>
                {SectionBreak}";

            // Insert after the tree view (approximate position, or just append)
            _content.Insert(_content.ToString().IndexOf("<body>") + 6, html);
        }

        public void AddReadme(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            string html = Markdown.ToHtml(markdown, pipeline);

            // Wrap in a container
            string section = $"<div class='doc-section'>{html}</div>{SectionBreak}";

            // Insert after Project Structure 
            // (Simpler approach: Append to a temporary buffer or insert at index)
            // For now, we append it, but Form1 controls the order.
            _content.Append(section);
        }

        public void AddMarkdown(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseGridTables().UsePipeTables().Build();
            string html = Markdown.ToHtml(markdown, pipeline);
            _content.Append($"<div class='doc-section'>{html}</div>{SectionBreak}");
        }

        public string GetHtml()
        {
            string html = _content.ToString();

            // Remove the very last section break to avoid a blank final page
            if (html.EndsWith(SectionBreak))
            {
                html = html.Substring(0, html.Length - SectionBreak.Length);
            }

            return html + "</body></html>";
        }
    }
}