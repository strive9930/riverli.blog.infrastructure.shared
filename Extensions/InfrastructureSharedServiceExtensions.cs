using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RiverLi.Blog.Infrastructure.Shared.Filters;
using RiverLi.Blog.Infrastructure.Shared.Middlewares;
using RiverLi.DDD.Core.Application.Common.Interfaces;
using RiverLi.Blog.Infrastructure.Shared.Auth;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions
{
    /// <summary>
    /// Infrastructure.Shared基础设施服务集成扩展
    /// </summary>
    public static class InfrastructureSharedServiceExtensions
    {
        /// <summary>
        /// 添加基础设施共享服务
        /// </summary>
        public static IServiceCollection AddInfrastructureSharedServices(
            this IServiceCollection services,
            Action<InfrastructureSharedOptions>? configure = null)
        {
            var options = new InfrastructureSharedOptions();
            configure?.Invoke(options);

            // 添加HttpContextAccessor
            services.AddHttpContextAccessor();

            // 添加当前用户服务
            services.AddScoped<ICurrentUser, CurrentUser>();

            // 添加控制器和过滤器
            var mvcBuilder = services.AddControllers(config =>
            {
                // 添加模型验证过滤器
                if (options.EnableAutoValidation)
                {
                    config.Filters.Add<ValidateModelStateFilter>();
                }
            });

            // 配置JSON序列化选项
            mvcBuilder.AddJsonOptions(opts =>
            {
                opts.JsonSerializerOptions.PropertyNamingPolicy =
                    options.UseJsonCamelCase ? System.Text.Json.JsonNamingPolicy.CamelCase : null;
                opts.JsonSerializerOptions.WriteIndented = options.JsonWriteIndented;
            });

            // 添加API版本控制
            if (options.EnableApiVersioning)
            {
                services.AddApiVersioningSupport();
            }

            // 添加CORS
            if (options.EnableCors)
            {
                services.AddCorsPolicy(options.AllowedOrigins);
            }

            // 添加响应压缩
            if (options.EnableResponseCompression)
            {
                services.AddResponseCompressionSupport();
            }

            // 添加OpenAPI文档 (修改这里)
            if (options.EnableOpenApiDocumentation) // 注意：这里的配置项 EnableScalar 仍然适用
            {
                services.AddOpenApiDocumentation( // 修改为调用 AddOpenApiDocumentation
                    options.ScalarTitle ?? "API",
                    options.ScalarVersion ?? "v1",
                    options.ScalarDescription,
                    options.ScalarEnableJwtAuth
                );
            }

            return services;
        }

        /// <summary>
        /// 使用基础设施共享中间件
        /// </summary>
        public static IApplicationBuilder UseInfrastructureSharedMiddlewares(
            this IApplicationBuilder app, // 注意：这里原来的 app 是 IApplicationBuilder
            Action<InfrastructureSharedOptions>? configure = null)
        {
            var options = new InfrastructureSharedOptions();
            configure?.Invoke(options);

            // 使用响应压缩
            if (options.EnableResponseCompression)
            {
                app.UseResponseCompression();
            }

            // 使用全局异常处理
            if (options.EnableGlobalExceptionHandler)
            {
                app.UseMiddleware<GlobalExceptionMiddleware>();
            }

            // 使用CORS
            if (options.EnableCors)
            {
                app.UseCorsPolicyMiddleware();
            }

            // --- 重要修改 ---
            // Scalar UI 需要在 WebApplication 级别配置，因为它使用了 Map 方法
            // 因此，UseOpenApiDocumentation 应该在 WebApplication 实例上调用，而不是在 IApplicationBuilder 上。
            // 所以这部分逻辑需要移到 Program.cs 或应用启动的地方。

            // 例如，在 Program.cs 中 (如果 app 是 WebApplication 类型):
            /*
             if (builder.Environment.IsDevelopment() || options.EnableScalar) // 可以根据需要调整条件
             {
                 app.UseOpenApiDocumentation(
                     options.ScalarTitle ?? "API",
                     options.ScalarVersion ?? "v1",
                     ScalarTheme.Moon // 或者从 options 传入主题
                 );
             }
            */

            // 这里保持为空或只放那些必须在 IApplicationBuilder 上调用的中间件
            // 当前没有需要在此处添加的 Scalar 相关中间件了。

            return app;
        }
        
        public static IServiceCollection AddHealthCheckSupport(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var builder = services.AddHealthChecks();
    
            // MySQL
            var mysqlConnection = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(mysqlConnection))
            {
                builder.AddMySql(
                    mysqlConnection,
                    name: "mysql",
                    failureStatus: HealthStatus.Degraded, // 数据库降级而非失败
                    tags: new[] { "db", "mysql" },
                    timeout: TimeSpan.FromSeconds(3)
                );
            }
    
            // Redis
            var redisConnection = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnection))
            {
                builder.AddRedis(
                    redisConnection,
                    name: "redis",
                    failureStatus: HealthStatus.Degraded, // 缓存降级
                    tags: new[] { "cache", "redis" },
                    timeout: TimeSpan.FromSeconds(2)
                );
            }
    
            // RabbitMQ
            var rabbitMqConnection = configuration.GetValue<string>("RabbitMq:Host");
            if (!string.IsNullOrEmpty(rabbitMqConnection))
            {
                builder.AddRabbitMQ(
                    name: "rabbitmq",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "messaging", "rabbitmq" }
                );
            }
    
            return services;
        }
        
        public static IServiceCollection AddLoggingSupport(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", "RiverLi.Blog")
                .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version)
                .Enrich.WithThreadId()
                .WriteTo.Console(new CompactJsonFormatter())
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    "logs/log-.json",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 100_000_000 // 100MB
                )
                .CreateLogger();
        
            services.AddSerilog();
            return services;
        }
    }

    /// <summary>
    /// 基础设施共享配置选项
    /// </summary>
    public class InfrastructureSharedOptions
    {
        /// <summary>
        /// 启用自动模型验证
        /// </summary>
        public bool EnableAutoValidation { get; set; } = true;

        /// <summary>
        /// 启用全局异常处理
        /// </summary>
        public bool EnableGlobalExceptionHandler { get; set; } = true;

        /// <summary>
        /// 启用API版本控制
        /// </summary>
        public bool EnableApiVersioning { get; set; } = false;

        /// <summary>
        /// 启用CORS跨域
        /// </summary>
        public bool EnableCors { get; set; } = true;

        /// <summary>
        /// 允许的跨域源（null表示允许所有）
        /// </summary>
        public string[]? AllowedOrigins { get; set; }

        /// <summary>
        /// 启用响应压缩
        /// </summary>
        public bool EnableResponseCompression { get; set; } = true;

        /// <summary>
        /// 启用Scalar文档 (注意：改名为更通用的)
        /// </summary>
        public bool EnableOpenApiDocumentation { get; set; } = true; // 建议改名，或者保留旧名但理解其用途

        /// <summary>
        /// Scalar标题
        /// </summary>
        public string? ScalarTitle { get; set; }

        /// <summary>
        /// Scalar版本
        /// </summary>
        public string? ScalarVersion { get; set; }

        /// <summary>
        /// Scalar描述
        /// </summary>
        public string? ScalarDescription { get; set; }

        /// <summary>
        /// Scalar启用JWT认证
        /// </summary>
        public bool ScalarEnableJwtAuth { get; set; } = true;

        /// <summary>
        /// 使用Scalar UI替代Scalar UI (这个配置项现在意义不大了，因为UI由MapScalarApiReference控制)
        /// </summary>
        public bool UseScalarUi { get; set; } = true; // 可以考虑移除或忽略

        /// <summary>
        /// JSON使用驼峰命名
        /// </summary>
        public bool UseJsonCamelCase { get; set; } = true;

        /// <summary>
        /// JSON格式化输出
        /// </summary>
        public bool JsonWriteIndented { get; set; } = false;
    }
}