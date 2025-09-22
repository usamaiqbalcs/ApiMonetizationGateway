using ApiMonetizationGateway.Redis.Service;
using ApiMonetizationGateway.Services.CustomerService;
using StackExchange.Redis;

#nullable disable
namespace ApiMonetizationGateway.Services.RateLimitingService.Impl
{
    public class RateLimitingService : IRateLimitingService
    {
        private readonly IRateLimitingStorage _storage;
        private readonly ICustomerService _customerService;

        public RateLimitingService( IRateLimitingStorage storage, ICustomerService customerService)
        {
            _storage = storage;
            _customerService = customerService;
        }
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
