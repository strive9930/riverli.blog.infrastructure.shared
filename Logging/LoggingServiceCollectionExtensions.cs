using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

namespace RiverLi.Blog.Infrastructure.Shared.Logging
{
    public static class LoggingServiceCollectionExtensions
    {
        /// <summary>
        /// 配置 Serilog 统一日志
        /// </summary>
        //public static void AddRiverLogging(this WebApplicationBuilder builder)
        //{
        //    Log.Logger = new LoggerConfiguration()
        //        .MinimumLevel.Information()
        //        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // 忽略微软自身的冗余日志
        //        .Enrich.FromLogContext()
        //        .Enrich.WithProperty("ApplicationName", builder.Environment.ApplicationName) // 区分是哪个服务的日志
        //        .WriteTo.Console()
        //        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        //        .CreateLogger();

        //    builder.Host.UseSerilog();
        //}
    }
}