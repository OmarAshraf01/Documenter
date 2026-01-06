using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Documenter
{
    public class GeminiAgent
    {
        // !!! PASTE YOUR NEW KEY HERE !!!
        private const string ApiKey = "AIzaSyBqxYv88YPxb1KV6KmrQtlX08m7d9vy8us";

        // VERSION SETTING: Using Gemini 2.0 Flash Experimental
        // (There is no 2.5 yet. This is the latest available.)
        private static readonly string ModelUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={ApiKey}";

        public static async Task<string> AnalyzeCode(HttpClient client, string fileName, string codeContent)
        {
            try
            {
                // Safety: Limit characters to avoid request size errors
                var safeCode = JsonConvert.ToString(codeContent.Substring(0, Math.Min(20000, codeContent.Length)));

                var prompt = $@"
                    Analyze this file: '{fileName}'.
                    1. LANGUAGE: Identify.
                    2. SUMMARY: Explain logic.
                    3. DIAGRAM: Mermaid code.

                    ## File: {fileName}
                    **Language**: [Lang]
                    **Summary**: [Summary]
                    
                    **Diagram**:
                    ```mermaid
                    [Mermaid Code]
                    ```
                ";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt + "\n\nCODE:\n" + safeCode } } }
                    }
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(ModelUrl, content);

                // ERROR CAPTURE: If this fails, the PDF will tell you EXACTLY why.
                if (!response.IsSuccessStatusCode)
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    return $"⚠️ API Error {response.StatusCode}: {errorDetails}";
                }

                string responseJson = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(responseJson);
                return data?.candidates?[0]?.content?.parts?[0]?.text ?? "No response.";
            }
            catch (Exception ex)
            {
                return $"❌ Connection Error: {ex.Message}";
            }
        }
    }
}