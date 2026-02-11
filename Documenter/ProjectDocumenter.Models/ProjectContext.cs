using System.Collections.Generic;

namespace ProjectDocumenter.Models
{
    /// <summary>
    /// Context for a project being analyzed
    /// </summary>
    public class ProjectContext
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        
        public List<string> FilesToAnalyze { get; set; } = new();
        public List<string> IgnoredFolders { get; set; } = new();
        public List<string> SupportedExtensions { get; set; } = new();
        
        public Dictionary<string, AnalysisResult> AnalysisCache { get; set; } = new();
        public Dictionary<string, string> Configuration { get; set; } = new();
    }
}
