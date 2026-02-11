using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectDocumenter.Services.Infrastructure
{
    /// <summary>
    /// Docker container management service
    /// </summary>
    public class DockerManager
    {
        private readonly ILogger<DockerManager> _logger;
        private const string DockerDesktopPath = @"C:\Program Files\Docker\Docker\Docker Desktop.exe";

        public DockerManager(ILogger<DockerManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> EnsureDockerRunningAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Checking Docker status...");

            // Check if Docker is installed
            if (!await RunCommandAsync("docker", "--version", cancellationToken))
            {
                _logger.LogError("Docker not found in PATH");
                return false;
            }

            // Check if Docker daemon is running
            if (await RunCommandAsync("docker", "ps", cancellationToken))
            {
                _logger.LogInformation("Docker is running");
                return true;
            }

            // Try to start Docker Desktop
            _logger.LogWarning("Docker is not running, attempting to start...");

            if (File.Exists(DockerDesktopPath))
            {
                Process.Start(new ProcessStartInfo(DockerDesktopPath) { UseShellExecute = true });

                // Wait for Docker to start (max 2 minutes)
                for (int i = 0; i < 24; i++)
                {
                    await Task.Delay(5000, cancellationToken);

                    if (await RunCommandAsync("docker", "ps", cancellationToken))
                    {
                        _logger.LogInformation("Docker started successfully");
                        return true;
                    }
                }

                _logger.LogError("Docker failed to start within timeout");
                return false;
            }

            _logger.LogError("Docker Desktop not found at: {Path}", DockerDesktopPath);
            return false;
        }

        public async Task<bool> EnsureContainerRunningAsync(
            string containerName,
            string image,
            string[] createArgs,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Checking container: {Container}", containerName);

            // Check if container exists
            bool exists = await RunCommandAsync("docker", $"inspect {containerName}", cancellationToken);

            if (!exists)
            {
                _logger.LogInformation("Creating container: {Container}", containerName);
                var createCommand = string.Join(" ", createArgs);
                await RunCommandAsync("docker", createCommand, cancellationToken);
            }

            // Check if running
            var isRunningCommand = $"ps --filter \"name={containerName}\" --filter \"status=running\" --format {{{{.Names}}}}";
            if (await RunCommandAsync("docker", isRunningCommand, cancellationToken))
            {
                _logger.LogInformation("Container is running");
                return true;
            }

            // Start the container
            _logger.LogInformation("Starting container: {Container}", containerName);
            return await RunCommandAsync("docker", $"start {containerName}", cancellationToken);
        }

        private async Task<bool> RunCommandAsync(string command, string args, CancellationToken cancellationToken)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return false;

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(outputTask, errorTask);
                await process.WaitForExitAsync(cancellationToken);

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Command failed: {Command} {Args}", command, args);
                return false;
            }
        }
    }
}
