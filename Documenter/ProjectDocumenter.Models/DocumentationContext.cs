using System.Collections.Generic;

namespace ProjectDocumenter.Models
{
    /// <summary>
    /// Context for generating documentation
    /// </summary>
    public class DocumentationContext
    {
        public string ProjectName { get; set; } = string.Empty;
        public string RepositoryUrl { get; set; } = string.Empty;
        public string ProjectTree { get; set; } = string.Empty;
        
        public List<AnalysisResult> AnalysisResults { get; set; } = new();
        public string DatabaseAnalysis { get; set; } = string.Empty;
        public string ReadmeContent { get; set; } = string.Empty;
        
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
