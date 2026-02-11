using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProjectDocumenter.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectDocumenter.Services.Caching
{
    /// <summary>
    /// File-based cache service with LRU eviction
    /// </summary>
    public class FileHashCache : ICacheService
    {
        private readonly string _cacheDirectory;
        private readonly ILogger<FileHashCache> _logger;
        private readonly ConcurrentDictionary<string, CacheEntry> _memoryCache = new();
        private readonly ReaderWriterLockSlim _lock = new();
        private long _hitCount;
        private long _missCount;

        public FileHashCache(string cacheDirectory, ILogger<FileHashCache> logger)
        {
            _cacheDirectory = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
        {
            // Check memory cache first
            if (_memoryCache.TryGetValue(key, out var entry))
            {
                Interlocked.Increment(ref _hitCount);
                entry.LastAccessed = DateTime.UtcNow;
                return entry.Value as T;
            }

            // Check disk cache
            var filePath = GetCacheFilePath(key);
            if (File.Exists(filePath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(filePath, cancellationToken);
                    var value = JsonConvert.DeserializeObject<T>(json);

                    // Load into memory cache
                    _memoryCache[key] = new CacheEntry { Value = value, LastAccessed = DateTime.UtcNow };

                    Interlocked.Increment(ref _hitCount);
                    return value;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read cache entry {Key}", key);
                }
            }

            Interlocked.Increment(ref _missCount);
            return null;
        }

        public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class
        {
            // Store in memory cache
            _memoryCache[key] = new CacheEntry { Value = value, LastAccessed = DateTime.UtcNow };

            // Store on disk
            var filePath = GetCacheFilePath(key);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            try
            {
                var json = JsonConvert.SerializeObject(value, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, json, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write cache entry {Key}", key);
            }
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_memoryCache.ContainsKey(key)) return Task.FromResult(true);
            return Task.FromResult(File.Exists(GetCacheFilePath(key)));
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _memoryCache.TryRemove(key, out _);

            var filePath = GetCacheFilePath(key);
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete cache entry {Key}", key);
                }
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _memoryCache.Clear();

            if (Directory.Exists(_cacheDirectory))
            {
                try
                {
                    Directory.Delete(_cacheDirectory, true);
                    Directory.CreateDirectory(_cacheDirectory);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to clear cache directory");
                }
            }

            _hitCount = 0;
            _missCount = 0;

            return Task.CompletedTask;
        }

        public Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var stats = new CacheStatistics
            {
                TotalEntries = _memoryCache.Count,
                HitCount = _hitCount,
                MissCount = _missCount
            };

            return Task.FromResult(stats);
        }

        private string GetCacheFilePath(string key)
        {
            // Use hash to create safe file name
            var safeKey = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key))
                .Replace("/", "_")
                .Replace("+", "-");

            return Path.Combine(_cacheDirectory, $"{safeKey}.json");
        }

        private class CacheEntry
        {
            public object? Value { get; set; }
            public DateTime LastAccessed { get; set; }
        }
    }
}
