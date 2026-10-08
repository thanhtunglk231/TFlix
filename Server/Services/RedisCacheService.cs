using Newtonsoft.Json;
using StackExchange.Redis;

namespace Server.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private static readonly JsonSerializerSettings SerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Include,
            DateParseHandling = DateParseHandling.None
        };

        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var value = await _redis.GetDatabase().StringGetAsync(key);
                if (!value.HasValue)
                {
                    _logger.LogInformation("Redis cache MISS for {CacheKey}", GetSafeCacheKey(key));
                    return default;
                }

                _logger.LogInformation("Redis cache HIT for {CacheKey}", GetSafeCacheKey(key));
                return JsonConvert.DeserializeObject<T>(value.ToString(), SerializerSettings);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis read failed for {CacheKey}; continuing without cache", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonConvert.SerializeObject(value, SerializerSettings);
                await _redis.GetDatabase().StringSetAsync(key, json, expiration);
                _logger.LogInformation(
                    "Redis cache SET for {CacheKey} with TTL {TtlSeconds} seconds",
                    GetSafeCacheKey(key),
                    (int)expiration.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis write failed for {CacheKey}; continuing without cache", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                await _redis.GetDatabase().KeyDeleteAsync(key);
                _logger.LogInformation("Redis cache DELETE for {CacheKey}", GetSafeCacheKey(key));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis delete failed for {CacheKey}", key);
            }
        }

        public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            try
            {
                var deletedCount = 0L;
                foreach (var endpoint in _redis.GetEndPoints())
                {
                    var server = _redis.GetServer(endpoint);
                    if (!server.IsConnected)
                        continue;

                    var keys = server.Keys(pattern: $"{prefix}*", pageSize: 250).ToArray();
                    if (keys.Length > 0)
                        deletedCount += await _redis.GetDatabase().KeyDeleteAsync(keys);
                }

                _logger.LogInformation(
                    "Redis cache INVALIDATE prefix {CachePrefix}; deleted {DeletedCount} keys",
                    prefix,
                    deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis prefix invalidation failed for {CachePrefix}", prefix);
            }
        }

        private static string GetSafeCacheKey(string key)
        {
            if (!key.StartsWith("tflix:otp:", StringComparison.OrdinalIgnoreCase))
                return key;

            var lastSeparator = key.LastIndexOf(':');
            return lastSeparator > 0 ? $"{key[..(lastSeparator + 1)]}***" : "tflix:otp:***";
        }
    }
}
