using StackExchange.Redis;

namespace Server.Services;

public sealed class RedisConnectionProvider : IRedisConnectionProvider, IAsyncDisposable
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ConnectAttemptTimeout = TimeSpan.FromSeconds(5);
    private readonly ConfigurationOptions _options;
    private readonly ILogger<RedisConnectionProvider> _logger;
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private IConnectionMultiplexer? _connection;
    private DateTimeOffset _nextRetryAt = DateTimeOffset.MinValue;

    public RedisConnectionProvider(ConfigurationOptions options, ILogger<RedisConnectionProvider> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<IConnectionMultiplexer?> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection?.IsConnected == true)
            return _connection;

        if (DateTimeOffset.UtcNow < _nextRetryAt)
            return null;

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection?.IsConnected == true)
                return _connection;

            if (DateTimeOffset.UtcNow < _nextRetryAt)
                return null;

            try
            {
                var connectTask = Task.Run<IConnectionMultiplexer>(
                    () => ConnectionMultiplexer.Connect(_options),
                    CancellationToken.None);
                IConnectionMultiplexer newConnection;
                try
                {
                    newConnection = await connectTask.WaitAsync(ConnectAttemptTimeout, cancellationToken);
                }
                catch (TimeoutException)
                {
                    _ = connectTask.ContinueWith(
                        static async task =>
                        {
                            if (task.Status == TaskStatus.RanToCompletion)
                                await task.Result.DisposeAsync();
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default).Unwrap();
                    throw new TimeoutException($"Redis connection attempt exceeded {ConnectAttemptTimeout.TotalSeconds} seconds.");
                }
                var oldConnection = Interlocked.Exchange(ref _connection, newConnection);
                if (oldConnection is not null)
                    await oldConnection.DisposeAsync();

                _nextRetryAt = DateTimeOffset.MinValue;
                _logger.LogInformation("Redis connection established to configured remote server");
                return newConnection;
            }
            catch (Exception exception)
            {
                _nextRetryAt = DateTimeOffset.UtcNow.Add(RetryDelay);
                _logger.LogWarning(exception, "Redis is unavailable; application will continue without cache and retry after {RetrySeconds} seconds", RetryDelay.TotalSeconds);
                return null;
            }
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async Task<IDatabase?> GetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        return connection?.GetDatabase();
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
        _connectLock.Dispose();
    }
}
