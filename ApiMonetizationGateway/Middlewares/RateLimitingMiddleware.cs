using ApiMonetizationGateway.Services.CustomerService;
using ApiMonetizationGateway.Services.RateLimitingService;
using ApiMonetizationGateway.Services.UsageTrackingService;

namespace ApiMonetizationGateway.Middlewares
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;

        public RateLimitingMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip rate limiting for certain endpoints
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/swagger"))
            {
                await _next(context);
                return;
            }

            var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
            if (string.IsNullOrEmpty(apiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API key required");
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var rateLimitingService = scope.ServiceProvider.GetRequiredService<IRateLimitingService>();
            var usageTrackingService = scope.ServiceProvider.GetRequiredService<IUsageTrackingService>();
            var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();

            var (isAllowed, message) = await rateLimitingService.CheckRateLimitAsync(apiKey, context.Request.Path);

            if (!isAllowed)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = "1";
                await context.Response.WriteAsync(message);
                return;
            }

            // Log successful request
            var customer = await customerService.GetCustomerByApiKeyAsync(apiKey);
            if (customer != null)
            {
                _ = Task.Run(() => usageTrackingService.LogUsageAsync(
                    customer.Id, context.Request.Path, 1, true));
            }

            await _next(context);
        }
    }
}
