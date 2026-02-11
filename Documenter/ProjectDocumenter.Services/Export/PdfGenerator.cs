using Microsoft.Extensions.Logging;
using ProjectDocumenter.Core.Interfaces;
using ProjectDocumenter.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectDocumenter.Services.Export
{
    /// <summary>
    /// PDF generator using PuppeteerSharp
    /// </summary>
    public class PdfGenerator : IDocumentGenerator
    {
        private readonly ILogger<PdfGenerator> _logger;
        private readonly string _browserPath;
        private static readonly SemaphoreSlim _browserDownloadLock = new(1, 1);

        public string Format => "PDF";

        public PdfGenerator(ILogger<PdfGenerator> logger, string? browserPath = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _browserPath = browserPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".local-chromium");
        }

        public async Task GenerateAsync(
            DocumentationContext context,
            string outputPath,
            IProgress<GenerationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // Ensure browser is downloaded
            await EnsureBrowserAsync(progress, cancellationToken);

            progress?.Report(new GenerationProgress
            {
                CurrentPhase = "Generating HTML",
                PercentComplete = 25,
                StatusMessage = "Building HTML document..."
            });

            // Generate HTML first
            var htmlGenerator = new HtmlDocumentBuilder(_logger);
            var html = htmlGenerator.Build(context);

            progress?.Report(new GenerationProgress
            {
                CurrentPhase = "Converting to PDF",
                PercentComplete = 50,
                StatusMessage = "Rendering PDF..."
            });

            // Find Chrome executable
            var chromePath = Directory.GetFiles(_browserPath, "chrome.exe", SearchOption.AllDirectories).FirstOrDefault()
                ?? Directory.GetFiles(_browserPath, "*.exe", SearchOption.AllDirectories)
                    .FirstOrDefault(f => f.Contains("chrome") || f.Contains("chromium"));

            if (string.IsNullOrEmpty(chromePath))
            {
                throw new FileNotFoundException("Chrome executable not found. Browser download may have failed.");
            }

            // Launch browser and generate PDF
            var launchOptions = new LaunchOptions
            {
                Headless = true,
                ExecutablePath = chromePath,
                Args = new[] { "--no-sandbox", "--disable-gpu", "--disable-dev-shm-usage" }
            };

            using var browser = await Puppeteer.LaunchAsync(launchOptions);
            using var page = await browser.NewPageAsync();

            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                Timeout = 0
            });

            // Wait a moment for any scripts to execute
            await Task.Delay(1000, cancellationToken);

            progress?.Report(new GenerationProgress
            {
                CurrentPhase = "Saving PDF",
                PercentComplete = 90,
                StatusMessage = "Finalizing document..."
            });

            await page.PdfAsync(outputPath, new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions
                {
                    Top = "20px",
                    Bottom = "20px",
                    Left = "20px",
                    Right = "20px"
                }
            });

            progress?.Report(new GenerationProgress
            {
                CurrentPhase = "Complete",
                PercentComplete = 100,
                StatusMessage = $"PDF generated: {outputPath}"
            });

            _logger.LogInformation("PDF generated successfully: {Path}", outputPath);
        }

        private async Task EnsureBrowserAsync(IProgress<GenerationProgress>? progress, CancellationToken cancellationToken)
        {
            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = _browserPath });

            // Check if already installed
            var installedBrowser = browserFetcher.GetInstalledBrowsers().FirstOrDefault();
            if (installedBrowser != null)
            {
                _logger.LogDebug("Browser already installed");
                return;
            }

            // Download browser (with lock to prevent concurrent downloads)
            await _browserDownloadLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check after acquiring lock
                installedBrowser = browserFetcher.GetInstalledBrowsers().FirstOrDefault();
                if (installedBrowser != null) return;

                _logger.LogInformation("Downloading browser...");
                progress?.Report(new GenerationProgress
                {
                    CurrentPhase = "Downloading Browser",
                    PercentComplete = 0,
                    StatusMessage = "Downloading Chromium..."
                });

                await browserFetcher.DownloadAsync();

                _logger.LogInformation("Browser download complete");
            }
            finally
            {
                _browserDownloadLock.Release();
            }
        }
    }
}
