using ApiMonetizationGateway.Redis.Service;
using ApiMonetizationGateway.Services.CustomerService;
using StackExchange.Redis;

#nullable disable
namespace ApiMonetizationGateway.Services.RateLimitingService.Impl
{
    public class RateLimitingService : IRateLimitingService
    {
        //private readonly IDatabase _redis;
        private readonly IRateLimitingStorage _storage;
        private readonly ICustomerService _customerService;

        public RateLimitingService(/*IConnectionMultiplexer redis,*/ IRateLimitingStorage storage, ICustomerService customerService)
        {
            //_redis = redis.GetDatabase();
            _storage = storage;
            _customerService = customerService;
        }

        //public async Task<(bool IsAllowed, string Message)> CheckRateLimitAsync(string apiKey, string endpoint)
        //{
        //    var customer = await _customerService.GetCustomerByApiKeyAsync(apiKey);
        //    if (customer == null || !customer.IsActive)
        //        return (false, "Invalid API key");

        //    var now = DateTime.UtcNow;
        //    var monthKey = $"quota:{customer.Id}:{now.Month}:{now.Year}";
        //    var rateKey = $"rate:{customer.Id}:{endpoint}";

        //    // Check monthly quota
        //    var monthlyUsage = await _redis.StringGetAsync(monthKey);
        //    if (monthlyUsage.HasValue && int.Parse(monthlyUsage) >= customer.Tier.MonthlyQuota)
        //        return (false, "Monthly quota exceeded");

        //    // Check rate limit using sliding window
        //    var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        //    var windowSize = 1; // 1 second

        //    var timestamps = await _redis.ListRangeAsync(rateKey);
        //    var validTimestamps = timestamps
        //        .Select(t => long.Parse(t))
        //        .Where(t => currentTime - t < windowSize)
        //        .ToList();

        //    if (validTimestamps.Count >= customer.Tier.RateLimit)
        //        return (false, "Rate limit exceeded");

        //    // Update rate limit
        //    await _redis.ListLeftPushAsync(rateKey, currentTime.ToString());
        //    await _redis.ListTrimAsync(rateKey, 0, customer.Tier.RateLimit - 1);
        //    await _redis.KeyExpireAsync(rateKey, TimeSpan.FromSeconds(windowSize));

        //    // Update monthly quota
        //    await _redis.StringIncrementAsync(monthKey);
        //    if (!monthlyUsage.HasValue)
        //    {
        //        var nextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
        //        var expiry = nextMonth - now;
        //        await _redis.KeyExpireAsync(monthKey, expiry);
        //    }

        //    return (true, "OK");
        //}
        public async Task<(bool IsAllowed, string Message)> CheckRateLimitAsync(string apiKey, string endpoint)
        {
            var customer = await _customerService.GetCustomerByApiKeyAsync(apiKey);
            if (customer == null || !customer.IsActive)
                return (false, "Invalid API key");

            var now = DateTime.UtcNow;
            var monthKey = $"quota:{customer.Id}:{now.Month}:{now.Year}";
            var rateKey = $"rate:{customer.Id}:{endpoint}";

            // Monthly quota
            var monthlyUsage = await _storage.StringIncrementAsync(monthKey) - 1;
            if (monthlyUsage >= customer.Tier.MonthlyQuota)
                return (false, "Monthly quota exceeded");

            // Rate limit
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var timestamps = await _storage.ListRangeAsync(rateKey);
            var validTimestamps = timestamps.Where(t => currentTime - t < 1).ToList();

            if (validTimestamps.Count >= customer.Tier.RateLimit)
                return (false, "Rate limit exceeded");

            // Update rate limit
            await _storage.ListLeftPushAsync(rateKey, currentTime);
            await _storage.ListTrimAsync(rateKey, 0, customer.Tier.RateLimit - 1);

            return (true, "OK");
        }
    }
}
