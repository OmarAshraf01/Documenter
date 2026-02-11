using System.Threading;
using System.Threading.Tasks;

namespace ProjectDocumenter.Core.Interfaces
{
    /// <summary>
    /// Abstraction for source code repositories (Git, local folders, SVN, etc.)
    /// </summary>
    public interface ISourceRepository
    {
        /// <summary>
        /// Clone/download the repository to the specified path
        /// </summary>
        Task<string> FetchAsync(string destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if the source is accessible
        /// </summary>
        Task<bool> IsAccessibleAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the repository name
        /// </summary>
        string GetName();

        /// <summary>
        /// Source type (Git, Local, SVN, etc.)
        /// </summary>
        string SourceType { get; }
    }
}
