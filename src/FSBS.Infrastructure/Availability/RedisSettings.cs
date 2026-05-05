namespace FSBS.Infrastructure.Availability;

public sealed class RedisSettings
{
    /// <summary>ElastiCache Redis connection string (host:port,ssl=true,...).</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
