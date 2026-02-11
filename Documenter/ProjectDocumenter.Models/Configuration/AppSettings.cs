namespace ProjectDocumenter.Models.Configuration
{
    /// <summary>
    /// Application configuration settings
    /// </summary>
    public class AppSettings
    {
        public AiProviderSettings AiProvider { get; set; } = new();
        public PerformanceSettings Performance { get; set; } = new();
        public ExportSettings Export { get; set; } = new();
        public CachingSettings Caching { get; set; } = new();
    }

    public class AiProviderSettings
    {
        public string Type { get; set; } = "Ollama"; // Ollama, OpenAI, Azure, Anthropic
        public string Endpoint { get; set; } = "http://localhost:11435";
        public string Model { get; set; } = "qwen2.5-coder:1.5b";
        public string ApiKey { get; set; } = string.Empty;
        public int MaxConcurrentRequests { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 300;
    }

    public class PerformanceSettings
    {
        public int MaxParallelFileAnalysis { get; set; } = 4;
        public int ChunkSizeBytes { get; set; } = 8192;
        public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
        public bool EnableStreaming { get; set; } = true;
    }

    public class ExportSettings
    {
        public string DefaultFormat { get; set; } = "PDF";
        public bool IncludeSourceCode { get; set; } = false;
        public string Theme { get; set; } = "modern-light";
        public bool GenerateHtml { get; set; } = true;
    }

    public class CachingSettings
    {
        public bool EnableCaching { get; set; } = true;
        public string CacheDirectory { get; set; } = ".cache";
        public int CacheTtlHours { get; set; } = 24;
        public long MaxCacheSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB
    }
}
