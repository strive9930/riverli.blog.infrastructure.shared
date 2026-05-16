using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions
{
    public static class RpcServiceCollectionExtensions
    {
        /// <summary>
        /// 添加声明式 RPC 客户端 (Refit) 并自动集成 Polly 重试/熔断
        /// </summary>
        /// <typeparam name="TInterface">Refit 接口定义</typeparam>
        /// <param name="serviceName">服务名称（用于从配置中获取 BaseAddress，或服务发现ID）</param>
        /// <param name="baseAddress">服务地址（如果配置中没写）</param>
        public static IHttpClientBuilder AddRiverRpcClient<TInterface>(
            this IServiceCollection services,
            string baseAddress)
            where TInterface : class
        {
            // 1. 定义 Polly 策略

            // 重试策略：网络抖动时重试 3 次
            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError() // 5xx, 408
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

            // 熔断策略：连续失败 5 次，熔断 30 秒
            var circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

            // 2. 注册 Refit Client
            return services.AddRefitClient<TInterface>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress))
                .AddPolicyHandler(retryPolicy)
                .AddPolicyHandler(circuitBreakerPolicy);
        }
    }
}
