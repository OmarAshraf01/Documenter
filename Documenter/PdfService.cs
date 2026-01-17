using PuppeteerSharp; // Requires 'PuppeteerSharp' NuGet package
using PuppeteerSharp.Media;
using System.Threading.Tasks;

namespace Documenter
{
    public class PdfService
    {
        public static async Task ConvertHtmlToPdf(string htmlContent, string outputPath)
        {
            // 1. Download Browser (One time setup)
            using var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            // 2. Launch Headless Chrome
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
            using var page = await browser.NewPageAsync();

            // 3. Set Content
            await page.SetContentAsync(htmlContent);

            // 4. Print to PDF
            await page.PdfAsync(outputPath, new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions { Top = "20px", Bottom = "20px", Left = "20px", Right = "20px" }
            });
        }
    }
}