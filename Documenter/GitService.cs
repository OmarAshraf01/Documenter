using System;
using System.Diagnostics;
using System.IO;

namespace Documenter
{
    public class GitService
    {
        public static void CloneRepository(string url, string targetPath)
        {
            var info = new ProcessStartInfo("git", $"clone {url} \"{targetPath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(info);
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception($"Git Error: {error}");
        }

        public static void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path)) return;
            // Recursively remove readonly attributes so we can delete
            foreach (var sub in Directory.GetDirectories(path)) DeleteDirectory(sub);
            foreach (var f in Directory.GetFiles(path))
            {
                File.SetAttributes(f, FileAttributes.Normal);
                File.Delete(f);
            }
            Directory.Delete(path);
        }
    }
}