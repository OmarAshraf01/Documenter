using System.Threading;
using System.Threading.Tasks;

namespace ProjectDocumenter.Core.Interfaces
{
    /// <summary>
    /// Caching service for AI responses and analysis results
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get cached value by key
        /// </summary>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Set cache value
        /// </summary>
        Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Check if key exists in cache
        /// </summary>
        Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove cached value
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clear all cache
        /// </summary>
        Task ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get cache statistics
        /// </summary>
        Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
    }

    public class CacheStatistics
    {
        public long TotalEntries { get; set; }
        public long HitCount { get; set; }
        public long MissCount { get; set; }
        public double HitRate => TotalEntries > 0 ? (double)HitCount / (HitCount + MissCount) : 0;
    }
}
