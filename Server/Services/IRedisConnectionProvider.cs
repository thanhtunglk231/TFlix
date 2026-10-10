using StackExchange.Redis;

namespace Server.Services;

public interface IRedisConnectionProvider
{
    Task<IConnectionMultiplexer?> GetConnectionAsync(CancellationToken cancellationToken = default);
    Task<IDatabase?> GetDatabaseAsync(CancellationToken cancellationToken = default);
}
