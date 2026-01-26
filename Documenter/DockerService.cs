using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Documenter
{
    public static class DockerService
    {
        private const string DockerDesktopPath = @"C:\Program Files\Docker\Docker\Docker Desktop.exe";

        // Matches the container name in your screenshot
        private const string ContainerName = "ai-server";
        private const string ModelName = "qwen2.5-coder:1.5b";

        // Port 11435 to avoid conflict with n8n
        private const string CreateContainerCmd = "run -d -v ollama:/root/.ollama -p 11435:11434 --name ai-server ollama/ollama";

        public static async Task<string> InitializeAsync(Action<string> logger)
        {
            try
            {
                logger("🐳 Checking Docker status...");
                if (!await RunCommandAsync("docker", "--version"))
                    return "❌ Docker not found in PATH.";

                // Check Daemon
                if (!await RunCommandAsync("docker", "ps"))
                {
                    logger("⏳ Starting Docker Desktop...");
                    if (File.Exists(DockerDesktopPath))
                    {
                        Process.Start(new ProcessStartInfo(DockerDesktopPath) { UseShellExecute = true });
                        int retries = 0;
                        while (!await RunCommandAsync("docker", "ps"))
                        {
                            await Task.Delay(5000);
                            if (++retries > 20) return "❌ Docker failed to start.";
                        }
                    }
                    else return "❌ Docker Desktop not found.";
                }

                logger($"🤖 Checking AI Container ({ContainerName})...");

                // Check if container exists
                bool containerExists = await RunCommandAsync("docker", $"inspect {ContainerName}");

                if (!containerExists)
                {
                    logger($"✨ Creating AI container on Port 11435...");
                    await RunCommandAsync("docker", CreateContainerCmd);
                }
                else
                {
                    // Check if running
                    bool isRunning = await RunCommandAsync("docker", $"ps --filter \"name={ContainerName}\" --filter \"status=running\" | findstr {ContainerName}");
                    if (!isRunning)
                    {
                        logger("▶️ Starting AI container...");
                        await RunCommandAsync("docker", $"start {ContainerName}");
                    }
                }

                // Check/Pull Model
                logger("🧠 Checking AI Model...");
                bool modelExists = await RunCommandAsync("docker", $"exec {ContainerName} ollama list | findstr \"{ModelName}\"");

                if (!modelExists)
                {
                    logger($"⬇️ Downloading Model ({ModelName})...");
                    logger("⏳ This may take a moment...");
                    await RunCommandAsync("docker", $"exec {ContainerName} ollama pull {ModelName}");
                }

                return "✅ AI System Ready (Port 11435).";
            }
            catch (Exception ex)
            {
                return $"❌ Error: {ex.Message}";
            }
        }

        // --- THE CRITICAL FIX IS HERE ---
        private static async Task<bool> RunCommandAsync(string command, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // We redirect this...
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return false;

                // FIX: Read BOTH streams at the same time.
                // If we don't read StandardError, the process hangs when the error buffer fills up.
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(outputTask, errorTask);
                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch { return false; }
        }
    }
}