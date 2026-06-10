// 3. 编写依赖注入一键打包扩展
using Microsoft.Extensions.DependencyInjection;

namespace RiverLi.Blog.Infrastructure.Shared.OpenApi
{
    public static class ApiSyncDependencyInjection
    {
        public static IServiceCollection AddApiSelfReporting(this IServiceCollection services)
        {
            services.AddHttpClient();
            // 挂载后台任务
            services.AddHostedService<ApiAutoSyncHostedService>();
            return services;
        }
    }
}