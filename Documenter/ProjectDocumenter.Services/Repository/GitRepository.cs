using Microsoft.Extensions.Logging;
using ProjectDocumenter.Core.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectDocumenter.Services.Repository
{
    /// <summary>
    /// Git repository implementation with shallow clone support
    /// </summary>
    public class GitRepository : ISourceRepository
    {
        private readonly string _url;
        private readonly ILogger<GitRepository> _logger;
        private readonly bool _shallowClone;

        public string SourceType => "Git";

        public GitRepository(string url, ILogger<GitRepository> logger, bool shallowClone = false)
        {
            _url = url ?? throw new ArgumentNullException(nameof(url));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _shallowClone = shallowClone;
        }

        public async Task<string> FetchAsync(string destination, CancellationToken cancellationToken = default)
        {
            if (Directory.Exists(destination))
            {
                _logger.LogInformation("Cleaning existing directory: {Dir}", destination);
                DeleteDirectory(destination);
            }

            Directory.CreateDirectory(destination);

            var args = _shallowClone
                ? $"clone --depth 1 {_url} \"{destination}\""
                : $"clone {_url} \"{destination}\"";

            _logger.LogInformation("Cloning repository: {Url}", _url);

            var psi = new ProcessStartInfo("git", args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start git process");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var error = await errorTask;
                throw new InvalidOperationException($"Git clone failed: {error}");
            }

            _logger.LogInformation("Repository cloned successfully");
            return destination;
        }

        public async Task<bool> IsAccessibleAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var psi = new ProcessStartInfo("git", $"ls-remote {_url}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return false;

                await process.WaitForExitAsync(cancellationToken);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public string GetName()
        {
            try
            {
                var uri = new Uri(_url);
                return uri.Segments[^1].TrimEnd('/').Replace(".git", "");
            }
            catch
            {
                return "Repository";
            }
        }

        private static void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path)) return;

            foreach (var dir in Directory.GetDirectories(path))
            {
                DeleteDirectory(dir);
            }

            foreach (var file in Directory.GetFiles(path))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            Directory.Delete(path);
        }
    }
}
