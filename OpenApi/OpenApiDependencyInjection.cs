// 路径：RiverLi.Blog.Infrastructure.Shared/OpenApi/OpenApiDependencyInjection.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using RiverLi.Blog.Infrastructure.Shared.Security;

namespace RiverLi.Blog.Infrastructure.Shared.OpenApi
{
    public static class OpenApiDependencyInjection
    {
        /// <summary>
        /// 统一封装的微服务 OpenAPI 注册
        /// </summary>
        public static IServiceCollection AddMicroserviceOpenApi(this IServiceCollection services, string documentName = "v1")
        {
            services.AddOpenApi(documentName, options =>
            {
                // 1. 注入我们上一轮写的 [AllowAnonymous] 转换器
                options.AddOperationTransformer<AllowAnonymousOperationTransformer>();

                // 2. 顺便统一配置所有微服务的 JWT 鉴权锁（极其实用！）
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Components ??= new OpenApiComponents();
                    
                    var securityScheme = new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "请输入 JWT Token，格式: Bearer {your_token}",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "Bearer",
                        BearerFormat = "JWT"
                    };

                    document.Components.SecuritySchemes.Add("Bearer", securityScheme);
                    return Task.CompletedTask;
                });
            });

            return services;
        }
    }
}