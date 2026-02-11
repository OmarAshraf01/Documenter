using System.Collections.Generic;

namespace ProjectDocumenter.Core.Interfaces
{
    /// <summary>
    /// RAG (Retrieval-Augmented Generation) service for context-aware code analysis
    /// </summary>
    public interface IRagService
    {
        /// <summary>
        /// Index a project's code for retrieval
        /// </summary>
        void IndexProject(string projectPath);

        /// <summary>
        /// Get relevant context for a given code snippet
        /// </summary>
        string GetContext(string code, int maxContextItems = 3);

        /// <summary>
        /// Clear the knowledge base
        /// </summary>
        void Clear();

        /// <summary>
        /// Get all indexed file names
        /// </summary>
        IReadOnlyList<string> GetIndexedFiles();
    }
}
