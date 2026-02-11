using Markdig;
using Microsoft.Extensions.Logging;
using ProjectDocumenter.Models;
using System;
using System.Linq;
using System.Text;

namespace ProjectDocumenter.Services.Export
{
    /// <summary>
    /// HTML document builder for documentation
    /// </summary>
    public class HtmlDocumentBuilder
    {
        private readonly ILogger _logger;
        private readonly MarkdownPipeline _markdownPipeline;

        public HtmlDocumentBuilder(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _markdownPipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseGridTables()
                .Build();
        }

        public string Build(DocumentationContext context)
        {
            var sb = new StringBuilder();

            // HTML Header
            sb.Append(@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>" + context.ProjectName + @" Documentation</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #24292e;
            max-width: 950px;
            margin: 0 auto;
            padding: 40px;
            font-size: 14px;
        }
        h1 {
            border-bottom: 2px solid #eaecef;
            padding-bottom: 5px;
            font-size: 26px;
            margin-top: 30px;
            color: #0366d6;
        }
        h2 {
            border-bottom: 1px solid #eaecef;
            padding-bottom: 5px;
            font-size: 20px;
            margin-top: 25px;
        }
        .tree-box {
            background-color: #f1f8ff;
            border: 1px solid #c8e1ff;
            border-radius: 5px;
            padding: 15px;
            font-family: 'Consolas', monospace;
            font-size: 13px;
            white-space: pre;
            overflow-x: auto;
        }
        .doc-section {
            margin-bottom: 30px;
        }
        .section-break {
            page-break-after: always;
            height: 1px;
            margin: 20px 0;
        }
        table {
            border-collapse: collapse;
            width: 100%;
            margin: 15px 0;
            font-size: 13px;
        }
        th, td {
            border: 1px solid #dfe2e5;
            padding: 8px 12px;
            text-align: left;
        }
        th {
            background-color: #f6f8fa;
            font-weight: 700;
        }
        tr:nth-child(2n) {
            background-color: #fcfcfc;
        }
        code {
            background-color: #f6f8fa;
            padding: 2px 5px;
            border-radius: 3px;
            font-family: 'Consolas', monospace;
        }
    </style>
</head>
<body>
");

            // Project Structure
            if (!string.IsNullOrEmpty(context.ProjectTree))
            {
                sb.Append(@"
<div class='doc-section'>
    <h1>📂 Project Structure</h1>
    <div class='tree-box'>" + context.ProjectTree + @"</div>
</div>
<div class='section-break'></div>
");
            }

            // README
            if (!string.IsNullOrEmpty(context.ReadmeContent))
            {
                var readmeHtml = Markdown.ToHtml(context.ReadmeContent, _markdownPipeline);
                sb.Append($"<div class='doc-section'>{readmeHtml}</div><div class='section-break'></div>");
            }

            // Database Analysis
            if (!string.IsNullOrEmpty(context.DatabaseAnalysis) && !context.DatabaseAnalysis.Contains("N/A"))
            {
                var dbHtml = Markdown.ToHtml(context.DatabaseAnalysis, _markdownPipeline);
                sb.Append($"<div class='doc-section'>{dbHtml}</div><div class='section-break'></div>");
            }

            // Code Analysis
            if (context.AnalysisResults.Any())
            {
                sb.Append("<hr/><h1>📘 Code Analysis</h1>");

                foreach (var result in context.AnalysisResults)
                {
                    var html = Markdown.ToHtml(result.MarkdownContent, _markdownPipeline);
                    sb.Append($"<div class='doc-section'>{html}</div><div class='section-break'></div>");
                }
            }

            sb.Append(@"
</body>
</html>
");

            return sb.ToString();
        }
    }
}
