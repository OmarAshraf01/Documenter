using Microsoft.Extensions.Logging;
using ProjectDocumenter.Core.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectDocumenter.Services.Repository
{
    /// <summary>
    /// Local folder repository implementation
    /// </summary>
    public class LocalFolderRepository : ISourceRepository
    {
        private readonly string _path;
        private readonly ILogger<LocalFolderRepository> _logger;

        public string SourceType => "LocalFolder";

        public LocalFolderRepository(string path, ILogger<LocalFolderRepository> logger)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<string> FetchAsync(string destination, CancellationToken cancellationToken = default)
        {
            // For local folders, just return the path
            if (!Directory.Exists(_path))
            {
                throw new DirectoryNotFoundException($"Directory not found: {_path}");
            }

            _logger.LogInformation("Using local folder: {Path}", _path);
            return Task.FromResult(_path);
        }

        public Task<bool> IsAccessibleAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Directory.Exists(_path));
        }

        public string GetName()
        {
            return Path.GetFileName(_path) ?? "LocalProject";
        }
    }
}
