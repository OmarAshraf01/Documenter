using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectDocumenter.Core.Interfaces;
using ProjectDocumenter.Models.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace ProjectDocumenter.Services.AI
{
    /// <summary>
    /// High-performance Ollama AI provider with connection pooling and streaming
    /// </summary>
    public class OllamaProvider : IAiProvider, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaProvider> _logger;
        private readonly AiProviderSettings _settings;
        private readonly string _generateUrl;

        public string ModelName => _settings.Model;

        public OllamaProvider(AiProviderSettings settings, ILogger<OllamaProvider> logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _generateUrl = $"{_settings.Endpoint.TrimEnd('/')}/api/generate";

            // Use SocketsHttpHandler for connection pooling
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = _settings.MaxConcurrentRequests
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds)
            };
        }

        public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                model = _settings.Model,
                prompt = prompt,
                stream = false
            };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_generateUrl, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("AI request failed: {StatusCode} - {Error}", response.StatusCode, error);
                    return $"AI Error: {response.ReasonPhrase}";
                }

                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                var json = JsonConvert.DeserializeObject<JObject>(responseText);

                if (json == null || json["response"] == null)
                {
                    _logger.LogWarning("Empty AI response received");
                    return "AI Error: Empty response.";
                }

                string result = json["response"]!.ToString();
                return CleanResponse(result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("AI request cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI request exception");
                return $"Error: AI Connection Failed - {ex.Message}";
            }
        }

        public async IAsyncEnumerable<string> StreamAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                model = _settings.Model,
                prompt = prompt,
                stream = true
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _generateUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line)) continue;

                var json = JsonConvert.DeserializeObject<JObject>(line);
                if (json?["response"] != null)
                {
                    yield return json["response"]!.ToString();
                }

                if (json?["done"]?.Value<bool>() == true) break;
            }
        }

        public async Task<IReadOnlyList<string>> GenerateBatchAsync(IEnumerable<string> prompts, CancellationToken cancellationToken = default)
        {
            var tasks = prompts.Select(p => GenerateAsync(p, cancellationToken));
            var results = await Task.WhenAll(tasks);
            return results;
        }

        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync(_settings.Endpoint, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static string CleanResponse(string response)
        {
            // Remove conversational fluff
            response = Regex.Replace(response, @"^Here is.*?:\s*", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            response = Regex.Replace(response, @"Note:.*", "", RegexOptions.IgnoreCase);
            response = Regex.Replace(response, @"^```[a-z]*\s*|\s*```$", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return response.Trim();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
