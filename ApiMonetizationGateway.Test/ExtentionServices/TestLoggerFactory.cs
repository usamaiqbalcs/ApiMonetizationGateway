using Microsoft.Extensions.Logging;

namespace ApiMonetizationGateway.Tests.ExtentionServices
{
    public static class TestLoggerFactory
    {
        public static ILogger<T> CreateLogger<T>()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole(); // Optional: logs appear in test output
            });
            return loggerFactory.CreateLogger<T>();
        }
    }
}
