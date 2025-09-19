using StackExchange.Redis;

namespace ApiMonetizationGateway.Redis.Service.Impl
{
    public class RedisRateLimitingStorage : IRateLimitingStorage
    {
        private readonly IDatabase _db;

        public RedisRateLimitingStorage(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task<long> StringIncrementAsync(string key) => await _db.StringIncrementAsync(key);

        public async Task<List<long>> ListRangeAsync(string key)
        {
            var values = await _db.ListRangeAsync(key);
            return values.Select(v => long.Parse(v)).ToList();
        }

        public async Task ListLeftPushAsync(string key, long value) => await _db.ListLeftPushAsync(key, value.ToString());

        public async Task ListTrimAsync(string key, int start, int end) => await _db.ListTrimAsync(key, start, end);
    }
}
