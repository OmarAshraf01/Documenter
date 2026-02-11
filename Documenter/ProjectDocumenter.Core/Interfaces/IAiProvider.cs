using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectDocumenter.Core.Interfaces
{
    /// <summary>
    /// Abstraction for AI providers (Ollama, OpenAI, Azure OpenAI, Anthropic, etc.)
    /// </summary>
    public interface IAiProvider
    {
        /// <summary>
        /// Send a prompt to the AI and get a response
        /// </summary>
        Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stream responses for real-time feedback
        /// </summary>
        IAsyncEnumerable<string> StreamAsync(string prompt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process multiple prompts in batch
        /// </summary>
        Task<IReadOnlyList<string>> GenerateBatchAsync(IEnumerable<string> prompts, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if the AI provider is available and healthy
        /// </summary>
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the model name being used
        /// </summary>
        string ModelName { get; }
    }
}
