using System;
using System.Diagnostics;
using System.IO;

namespace Documenter
{
    public class GitService
    {
        public static void CloneRepository(string url, string targetPath)
        {
            // Update: We now capture the ERROR message from Git to show it properly.
            var info = new ProcessStartInfo("git", $"clone {url} \"{targetPath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true, // Capture errors
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(info);

            // Read the output strings
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            // If Git failed, show the ACTUAL error message
            if (process.ExitCode != 0)
            {
                throw new Exception($"Git Error: {error}");
            }
        }

        public static void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path)) return;

            foreach (var subDir in Directory.GetDirectories(path)) DeleteDirectory(subDir);
            foreach (var file in Directory.GetFiles(path))
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }
            Directory.Delete(path);
        }
    }
}