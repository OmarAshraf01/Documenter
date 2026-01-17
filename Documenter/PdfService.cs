using PuppeteerSharp;
using PuppeteerSharp.Media;
using System;
using System.Threading.Tasks;

namespace Documenter
{
    public class PdfService
    {
        public static async Task ConvertHtmlToPdf(string htmlContent, string outputPath)
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            var options = new LaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-gpu",
                    "--disable-dev-shm-usage"
                }
            };

            using var browser = await Puppeteer.LaunchAsync(options);
            using var page = await browser.NewPageAsync();

            // 1. Load Content (Simple Load, no strict network idle check)
            await page.SetContentAsync(htmlContent, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Load },
                Timeout = 0 // Disable timeout for loading
            });

            // 2. Dumb Wait (Reliable): Wait 3 seconds for Mermaid.js diagrams to draw
            await Task.Delay(3000);

            // 3. Render PDF
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
        }
    }
}