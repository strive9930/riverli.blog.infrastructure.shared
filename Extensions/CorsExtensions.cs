using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions
{
    /// <summary>
    /// CORS跨域配置扩展
    /// </summary>
    public static class CorsExtensions
    {
        private const string DefaultPolicyName = "DefaultCorsPolicy";

        /// <summary>
        /// 添加CORS跨域支持
        /// </summary>
        public static IServiceCollection AddCorsPolicy(
            this IServiceCollection services,
            string[]? allowedOrigins = null,
            string policyName = DefaultPolicyName)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(policyName, builder =>
                {
                    if (allowedOrigins != null && allowedOrigins.Length > 0)
                    {
                        // 指定允许的源
                        builder.WithOrigins(allowedOrigins);
                    }
                    else
                    {
                        // 开发环境允许所有源
                        builder.AllowAnyOrigin();
                    }

                    builder
                        .AllowAnyMethod()
                        .AllowAnyHeader();

                    // 如果指定了源，则允许凭据
                    if (allowedOrigins != null && allowedOrigins.Length > 0)
                    {
                        builder.AllowCredentials();
                    }
                });
            });

            return services;
        }

        /// <summary>
        /// 使用CORS策略
        /// </summary>
        public static IApplicationBuilder UseCorsPolicyMiddleware(
            this IApplicationBuilder app,
            string policyName = DefaultPolicyName)
        {
            app.UseCors(policyName);
            return app;
        }
    }
}