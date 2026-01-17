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
                    "--disable-gpu", // Prevents Protocol Error
                    "--disable-dev-shm-usage"
                }
            };

            using var browser = await Puppeteer.LaunchAsync(options);
            using var page = await browser.NewPageAsync();

            await page.SetContentAsync(htmlContent, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            // Wait for diagrams (with safety timeout)
            try
            {
                await page.WaitForSelectorAsync(".mermaid", new WaitForSelectorOptions { Timeout = 3000 });
            }
            catch { }

            await page.PdfAsync(outputPath, new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions { Top = "20px", Bottom = "20px", Left = "20px", Right = "20px" }
            });
        }
    }
}