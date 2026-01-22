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
        // Path logic shared between methods
        private static string _browserFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".local-chromium");

        // 1. This runs in the background while AI is working
        public static async Task PrepareBrowserAsync(Action<string> logger)
        {
            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = _browserFolder });

            // Check if already installed
            var installedBrowser = browserFetcher.GetInstalledBrowsers().FirstOrDefault();
            if (installedBrowser != null) return; // Already ready, exit fast.

            logger("⬇️ [Background] Starting Browser Download...");

            // Heartbeat to show it's alive
            var cts = new System.Threading.CancellationTokenSource();
            var heartbeat = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(8000); // Notify every 8 seconds
                    if (!cts.Token.IsCancellationRequested) try { logger("... [Background] Still downloading Browser ..."); } catch { }
                }
            }, cts.Token);

            try
            {
                await browserFetcher.DownloadAsync();
                logger("✅ [Background] Browser Download Complete.");
            }
            catch (Exception ex)
            {
                logger($"❌ [Background] Download Failed: {ex.Message}");
            }
            finally
            {
                cts.Cancel();
            }
        }

        // 2. This runs at the very end
        public static async Task ConvertHtmlToPdf(string htmlContent, string outputPath)
        {
            // Find the executable (It should be there now!)
            string? chromePath = Directory.GetFiles(_browserFolder, "chrome.exe", SearchOption.AllDirectories).FirstOrDefault();

            if (string.IsNullOrEmpty(chromePath))
            {
                // Fallback search
                chromePath = Directory.GetFiles(_browserFolder, "*.exe", SearchOption.AllDirectories)
                                      .FirstOrDefault(f => f.Contains("chrome") || f.Contains("chromium"));

                if (string.IsNullOrEmpty(chromePath))
                    throw new FileNotFoundException("Chrome executable not found. The background download might have failed.");
            }

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
                Timeout = 0
            });

            // Wait for Mermaid
            try { await page.WaitForSelectorAsync(".mermaid svg", new WaitForSelectorOptions { Timeout = 3000 }); } catch { }

            await Task.Delay(1000);

            await page.PdfAsync(outputPath, new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions { Top = "20px", Bottom = "20px", Left = "20px", Right = "20px" }
            });
        }
    }
}