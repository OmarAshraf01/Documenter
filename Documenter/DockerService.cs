using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Documenter
{
    public static class DockerService
    {
        // Standard path for Docker Desktop on Windows
        private const string DockerDesktopPath = @"C:\Program Files\Docker\Docker\Docker Desktop.exe";
        private const string ContainerName = "ai-server";

        // This is the command used to create the container if it doesn't exist
        private const string CreateContainerCmd = "run -d -v ollama:/root/.ollama -p 11434:11434 --name ai-server ollama/ollama";

        public static async Task<string> InitializeAsync(Action<string> logger)
        {
            try
            {
                // 1. Check if Docker is installed/accessible
                logger("🐳 Checking Docker status...");
                if (!await RunCommandAsync("docker", "--version"))
                {
                    return "❌ Docker is not found in PATH. Please install Docker Desktop.";
                }

                // 2. Check if Docker Daemon is running
                bool isDaemonRunning = await RunCommandAsync("docker", "ps");
                if (!isDaemonRunning)
                {
                    logger("⏳ Docker is not running. Launching Docker Desktop...");
                    if (File.Exists(DockerDesktopPath))
                    {
                        Process.Start(new ProcessStartInfo(DockerDesktopPath) { UseShellExecute = true });

                        // Wait for Docker to warm up (polling)
                        logger("⏳ Waiting for Docker to start (this may take a minute)...");
                        int retries = 0;
                        while (!await RunCommandAsync("docker", "ps"))
                        {
                            await Task.Delay(5000); // Wait 5 seconds
                            retries++;
                            if (retries > 20) return "❌ Docker Desktop failed to start in time.";
                        }
                    }
                    else
                    {
                        return "❌ Could not find Docker Desktop.exe at standard location.";
                    }
                }

                // 3. Check/Start the specific AI Container
                logger("🤖 Checking AI Container...");

                // Check if container exists (running or stopped)
                bool containerExists = await RunCommandAsync("docker", $"inspect {ContainerName}");

                if (!containerExists)
                {
                    logger("✨ Creating new AI container...");
                    await RunCommandAsync("docker", CreateContainerCmd);
                    // Need to pull the model if it's a fresh container
                    logger("⬇️ Downloading AI Model (qwen2.5-coder:1.5b)...");
                    await RunCommandAsync("docker", $"exec {ContainerName} ollama run qwen2.5-coder:1.5b");
                }
                else
                {
                    // Check if it's currently running
                    bool isRunning = await RunCommandAsync("docker", $"ps --filter \"name={ContainerName}\" --filter \"status=running\" | findstr {ContainerName}");

                    if (!isRunning)
                    {
                        logger("▶️ Starting existing AI container...");
                        await RunCommandAsync("docker", $"start {ContainerName}");
                    }
                }

                return "✅ AI System Ready.";
            }
            catch (Exception ex)
            {
                return $"❌ Error initializing Docker: {ex.Message}";
            }
        }

        private static async Task<bool> RunCommandAsync(string command, string args)
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

                // Read output to ensure command worked
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                // For 'docker ps' or 'docker inspect', exit code 0 means success/found
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}