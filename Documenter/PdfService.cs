using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Documenter
{
    public class PdfService
    {
        public static async Task ConvertHtmlToPdf(string htmlContent, string outputPath, Action<string> logger)
        {
            // 1. Setup paths
            string exeFolder = AppDomain.CurrentDomain.BaseDirectory;
            string browserFolder = Path.Combine(exeFolder, ".local-chromium");

            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions
            {
                Path = browserFolder
            });

            // 2. Check if we need to download
            var installedBrowser = browserFetcher.GetInstalledBrowsers().FirstOrDefault();

            if (installedBrowser == null)
            {
                logger("⬇️ Browser not found. Starting download (approx 170MB)...");
                logger("⏳ This happens only once. Please wait...");

                // --- HEARTBEAT LOGIC START ---
                // This will print a log every 5 seconds so you know it's not frozen
                var cts = new System.Threading.CancellationTokenSource();
                var heartbeat = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(5000);
                        if (!cts.Token.IsCancellationRequested)
                        {
                            // Use Invoke pattern if needed, but logger usually handles string safely
                            try { logger("... Still downloading (Do not close) ..."); } catch { }
                        }
                    }
                }, cts.Token);
                // --- HEARTBEAT LOGIC END ---

                try
                {
                    await browserFetcher.DownloadAsync();
                }
                finally
                {
                    cts.Cancel(); // Stop the heartbeat messages
                }

                logger("✅ Download Complete.");
            }
            else
            {
                logger("✅ Browser already found. Skipping download.");
            }

            // 3. Locate the executable
            string? chromePath = Directory.GetFiles(browserFolder, "chrome.exe", SearchOption.AllDirectories).FirstOrDefault();

            if (string.IsNullOrEmpty(chromePath))
            {
                // Fallback search
                chromePath = Directory.GetFiles(browserFolder, "*.exe", SearchOption.AllDirectories)
                                      .FirstOrDefault(f => f.Contains("chrome") || f.Contains("chromium"));

                if (string.IsNullOrEmpty(chromePath))
                    throw new FileNotFoundException($"Chrome executable not found in {browserFolder}.");
            }

            // 4. Launch
            var options = new LaunchOptions
            {
                Headless = true,
                ExecutablePath = chromePath,
                Args = new[] { "--no-sandbox", "--disable-gpu" }
            };

            using var browser = await Puppeteer.LaunchAsync(options);
            using var page = await browser.NewPageAsync();

            await page.SetContentAsync(htmlContent, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                Timeout = 0 // Disable timeout
            });

            // Small delay for layout
            await Task.Delay(1000);

            // 5. Save PDF
            await page.PdfAsync(outputPath, new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions { Top = "20px", Bottom = "20px", Left = "20px", Right = "20px" }
            });
        }
    }
}