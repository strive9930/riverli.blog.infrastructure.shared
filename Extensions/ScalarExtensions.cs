using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Reflection;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions
{
    /// <summary>
    /// scalar文档配置扩展
    /// </summary>
    public static class ScalarExtensions
    {
        /// <summary>
        /// 添加OpenAPI文档生成和Scalar UI配置
        /// </summary>
        public static IServiceCollection AddOpenApiDocumentation(
            this IServiceCollection services,
            string title,
            string version = "v1",
            string? description = null,
            bool enableJwtAuth = true,
            List<OpenApiServer>? servers = null)
        {
            // 使用新的OpenAPI配置方式
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    // 设置文档基本信息
                    document.Info = new OpenApiInfo
                    {
                        Title = title,
                        Version = version,
                        Description = description ?? $"{title} API 文档",
                        Contact = new OpenApiContact
                        {
                            Name = "RiverLi",
                            Email = "contact@riverli.com"
                        }
                    };

                    // 设置服务器入口点，如果提供了则使用
                    if (servers != null && servers.Any())
                    {
                        document.Servers = servers;
                    }

                    document.Components ??= new OpenApiComponents();

                    // 启用JWT认证
                    if (enableJwtAuth)
                    {
                        var schemeName = "Bearer";
                        document.Components.SecuritySchemes.Add(schemeName, new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer",
                            BearerFormat = "JWT",
                            In = ParameterLocation.Header,
                            Description = "请输入 JWT Token"
                        });

                        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = schemeName }
                            }] = Array.Empty<string>()
                        });
                    }

                    // 读取XML注释
                    var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml");
                    foreach (var xmlFile in xmlFiles)
                    {
                        // 这里无法直接添加XML注释到OpenAPI文档中，仍需使用SwaggerGen
                    }

                    return Task.CompletedTask;
                });
            });
            return services;
        }

        /// <summary>
        /// 使用OpenAPI文档和Scalar UI
        /// </summary>
        public static IApplicationBuilder UseOpenApiDocumentation(
            this WebApplication app,
            string title = "API",
            string version = "v1",
            ScalarTheme theme = ScalarTheme.Moon)
        {
            // 生成OpenAPI JSON文档
            app.MapOpenApi();

            // 使用Scalar UI
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle(title)
                    .WithTheme(theme)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });

            return app;
        }
    }
}