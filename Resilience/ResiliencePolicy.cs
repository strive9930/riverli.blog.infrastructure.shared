using Polly;
using Polly.Extensions.Http;

public static class ResiliencePolicy
{
    // 定义一个标准重试策略：重试3次，间隔按 2^n 指数增长
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // 处理 5xx 或 408
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound) // 可选：处理 404
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}