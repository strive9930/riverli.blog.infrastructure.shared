using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using RiverLi.Blog.Infrastructure.Shared.Logging;
using Microsoft.Extensions.Configuration;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions
{
    /// <summary>
    /// 核心扩展实现：高度可配置的日志方案，支持异步写入、结构化属性和全链路追踪
    /// </summary>
    public static class LoggingExtensions
    {
        public static void AddRiverLogging(this WebApplicationBuilder builder)
        {
            var options = builder.Configuration.GetSection(LoggingOptions.SectionName).Get<LoggingOptions>() ?? new LoggingOptions();

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(Enum.Parse<LogEventLevel>(options.MinimumLevel))
                // 排除微软底层无关日志
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                // 包含上下文属性
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                // 输出到控制台
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                // 输出到文件（JSON 格式，方便日志采集系统如 ELK 读取）
                .WriteTo.File(new JsonFormatter(), options.LogPath, rollingInterval: RollingInterval.Day);

            // 如果配置了 Seq (一款非常棒的结构化日志查看工具)
            if (!string.IsNullOrEmpty(options.SeqUrl))
            {
                loggerConfig.WriteTo.Seq(options.SeqUrl);
            }

            Log.Logger = loggerConfig.CreateLogger();
            builder.Host.UseSerilog();
        }
    }
}
