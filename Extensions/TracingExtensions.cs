using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions
{
    public static class TracingExtensions
    {
        public static IServiceCollection AddRiverTracing(this IServiceCollection services,
            string serviceName, string jaegerHost = "localhost")
        {
            services.AddOpenTelemetry()
                .WithTracing(builder =>
                {
                    builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                        // 1. 监听来自 ASP.NET Core 的请求
                        .AddAspNetCoreInstrumentation()
                        // 2. 监听 HttpClient 的外部调用 (RPC)
                        .AddHttpClientInstrumentation()
                        // 3. 监听 SQL 执行情况 (EF Core)
                        .AddEntityFrameworkCoreInstrumentation()
                        // 4. 导出数据到 Jaeger via OTLP
                        .AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri($"http://{jaegerHost}:4317");
                         });
                 });
            return services;
        }
    }
}