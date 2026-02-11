namespace ProjectDocumenter.Models
{
    /// <summary>
    /// Progress information for analysis
    /// </summary>
    public class AnalysisProgress
    {
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public string CurrentFile { get; set; } = string.Empty;
        public string CurrentPhase { get; set; } = string.Empty;
        public double PercentComplete => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
    }

    /// <summary>
    /// Progress information for document generation
    /// </summary>
    public class GenerationProgress
    {
        public string CurrentPhase { get; set; } = string.Empty;
        public int PercentComplete { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }
}
