using Asp.Versioning;
using Asp.Versioning.ApiExplorer; 
using Microsoft.Extensions.DependencyInjection;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions
{
    /// <summary>
    /// API版本控制扩展
    /// </summary>
    public static class ApiVersioningExtensions
    {
        /// <summary>
        /// 添加API版本控制
        /// </summary>
        public static IServiceCollection AddApiVersioningSupport(this IServiceCollection services)
        {
            services.AddApiVersioning(options =>
            {
                // 配置 API 版本控制
                services.AddApiVersioning(options =>
                {
                    // 默认版本
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                    // 假定默认版本（当请求中未指定版本时）
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    // 在响应头中报告 API 版本信息
                    options.ReportApiVersions = true;
                    // 定义从何处读取 API 版本信息：URL 路径段、查询字符串参数 'api-version'、请求头 'X-API-Version'
                    options.ApiVersionReader = ApiVersionReader.Combine(
                        new UrlSegmentApiVersionReader(),      // 例如: /api/v1/controller/action
                        new QueryStringApiVersionReader("api-version"), // 例如: /api/controller/action?api-version=1.0
                        new HeaderApiVersionReader("X-API-Version")     // 例如: 请求头 X-API-Version: 1.0
                    );
                });

                // 注意：不再需要 services.AddVersionedApiExplorer(options => ...) 
                // Asp.Versioning.Mvc 会自动与 API Explorer 集成。
            });

            // 配置 API 探索器以支持版本化
            // services.AddVersionedApiExplorer(options =>
            // {
            //     // 定义版本组名称的格式，例如 'v1', 'v2'
            //     options.GroupNameFormat = "'v'VVV";
            //     // 允许在生成的 URL 中替换 API 版本占位符
            //     options.SubstituteApiVersionInUrl = true;
            // });

            return services;
        }
    }
}