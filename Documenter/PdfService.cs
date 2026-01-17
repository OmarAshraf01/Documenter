using PuppeteerSharp;
using PuppeteerSharp.Media;
using System.Threading.Tasks;

namespace Documenter
{
    public class PdfService
    {
        public static async Task ConvertHtmlToPdf(string htmlContent, string outputPath)
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            using var page = await browser.NewPageAsync();

            await page.SetContentAsync(htmlContent);

            // Wait for Mermaid Diagram to render (important!)
            // We wait for the div with class 'mermaid' to exist
            try { await page.WaitForSelectorAsync(".mermaid", new WaitForSelectorOptions { Timeout = 2000 }); }
            catch { /* Continue if no diagram */ }

            // Small buffer to ensure rendering matches styles
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