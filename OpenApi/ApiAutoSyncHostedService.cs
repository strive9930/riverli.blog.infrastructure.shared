// 2. 编写后台上报服务
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace RiverLi.Blog.Infrastructure.Shared.OpenApi
{
    public class ApiAutoSyncHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiAutoSyncHostedService> _logger;

        public ApiAutoSyncHostedService(
            IServiceProvider serviceProvider, 
            IHostApplicationLifetime lifetime, 
            IConfiguration configuration,
            ILogger<ApiAutoSyncHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _lifetime = lifetime;
            _configuration = configuration;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // 🌟 订阅程序完全启动后的事件（此时 Kestrel 已就绪，路由树已完全建立）
            _lifetime.ApplicationStarted.Register(OnApplicationStarted);
            return Task.CompletedTask;
        }

        private async void OnApplicationStarted()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var endpointDataSource = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                // 从各微服务的 appsettings.json 读取微服务名称和身份中心地址
                var serviceName = _configuration["ServiceName"] ?? AppDomain.CurrentDomain.FriendlyName;
                var identityUrl = _configuration["Services:IdentityUrl"] ?? "http://localhost:5001"; 

                var apiResources = new List<ReportApiDto>();

                // 🌟 核心绝招：直接在内存中扫描点对点路由树，完全不依赖外部 HTTP 请求
                foreach (var endpoint in endpointDataSource.Endpoints)
                {
                    if (endpoint is RouteEndpoint routeEndpoint)
                    {
                        // 抓取 Controller 上下文
                        var actionDescriptor = routeEndpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
                        if (actionDescriptor == null) continue; // 自动过滤掉 Scalar UI、健康检查等系统级端点

                        var routePattern = routeEndpoint.RoutePattern.RawText ?? "";
                        if (!routePattern.StartsWith("/")) routePattern = "/" + routePattern;

                        // 提取 HTTP 方法 (GET/POST/PUT/DELETE)
                        var httpMethodMetadata = routeEndpoint.Metadata.GetMetadata<HttpMethodMetadata>();
                        var methods = httpMethodMetadata?.HttpMethods ?? new[] { "GET" };

                        // 检查是否挂了 [AllowAnonymous]
                        var isPublic = routeEndpoint.Metadata.GetMetadata<IAllowAnonymous>() != null;
                        
                        // 提取接口描述（优先读取 Swagger 的 EndpointDescription，其次降级为 Action 名字）
                        var description = routeEndpoint.Metadata.GetMetadata<EndpointDescriptionAttribute>()?.Description 
                                          ?? $"{actionDescriptor.ControllerName} - {actionDescriptor.ActionName}";

                        foreach (var method in methods)
                        {
                            apiResources.Add(new ReportApiDto
                            {
                                Method = method.ToUpper(),
                                Route = routePattern,
                                Description = description,
                                IsPublic = isPublic
                            });
                        }
                    }
                }

                if (!apiResources.Any()) return;

                // 🌟 将收割到的本服务全量 API，一发 POST 请求推给 Identity 统一权限中心
                var client = httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync($"{identityUrl}/api/identity/api-resources/report?serviceName={serviceName}", apiResources);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"[鉴权基建] 成功自动上报 【{serviceName}】微服务共 {apiResources.Count} 个 API 接口至权限中心。");
                }
                else
                {
                    _logger.LogWarning($"[鉴权基建] 自动上报 API 失败，权限中心返回状态码: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[鉴权基建] 执行微服务 API 自动上报时发生异常。");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}