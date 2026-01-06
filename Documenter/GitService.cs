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
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception("Git clone failed. Check the URL.");
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