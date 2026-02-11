using System;
using System.Collections.Generic;

namespace ProjectDocumenter.Models
{
    /// <summary>
    /// Result of analyzing a single code file
    /// </summary>
    public class AnalysisResult
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string MarkdownContent { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public string FileHash { get; set; } = string.Empty;
        
        public List<CodeComponent> Components { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class CodeComponent
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Class, Interface, Function, etc.
        public string Description { get; set; } = string.Empty;
        public int StartLine { get; set; }
        public int EndLine { get; set; }
    }
}
