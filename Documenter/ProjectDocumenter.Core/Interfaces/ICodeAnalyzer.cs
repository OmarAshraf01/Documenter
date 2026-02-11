using ProjectDocumenter.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectDocumenter.Core.Interfaces
{
    /// <summary>
    /// Code analysis service with streaming support for large codebases
    /// </summary>
    public interface ICodeAnalyzer
    {
        /// <summary>
        /// Analyze a single file
        /// </summary>
        Task<AnalysisResult> AnalyzeFileAsync(string filePath, string content, ProjectContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyze multiple files with streaming results (memory-efficient)
        /// </summary>
        IAsyncEnumerable<AnalysisResult> AnalyzeProjectAsync(ProjectContext context, IProgress<AnalysisProgress>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyze only files that have changed since last analysis (incremental)
        /// </summary>
        IAsyncEnumerable<AnalysisResult> AnalyzeIncrementalAsync(ProjectContext context, IProgress<AnalysisProgress>? progress = null, CancellationToken cancellationToken = default);
    }
}
