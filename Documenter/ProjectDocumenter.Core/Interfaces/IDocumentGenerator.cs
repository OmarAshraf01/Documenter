using ProjectDocumenter.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectDocumenter.Core.Interfaces
{
    /// <summary>
    /// Document generation abstraction supporting multiple formats
    /// </summary>
    public interface IDocumentGenerator
    {
        /// <summary>
        /// Generate documentation from analysis results
        /// </summary>
        Task GenerateAsync(DocumentationContext context, string outputPath, IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Supported output format
        /// </summary>
        string Format { get; }
    }
}
