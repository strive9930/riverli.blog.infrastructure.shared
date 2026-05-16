using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HealthChecks.UI.Client;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions
{
    /// <summary>
    /// 全栈健康检查 (Health Checks)：web服务、数据库、Redis、RabbitMQ、Elasticsearch 是否正常
    /// </summary>
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddRiverHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var hcBuilder = services.AddHealthChecks();

            // 1. 检查数据库 (从配置读取连接字符串)
            var mysqlConn = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(mysqlConn))
                hcBuilder.AddMySql(mysqlConn, name: "MySQL");

            // 2. 检查 Redis
            var redisConn = configuration.GetSection("Redis:ConnectionString").Value;
            if (!string.IsNullOrEmpty(redisConn))
                hcBuilder.AddRedis(redisConn, name: "Redis");

            // 3. 检查 RabbitMQ
            var rabbitOptions = configuration.GetSection("RabbitMq");
            if (rabbitOptions.Exists())
            {
                var connectionFactory = new RabbitMQ.Client.ConnectionFactory()
                {
                    HostName = rabbitOptions["Host"],
                    UserName = rabbitOptions["Username"],
                    Password = rabbitOptions["Password"],
                    Port = int.Parse(rabbitOptions["Port"] ?? "5672")
                };
                var rabbitUri = $"amqp://{rabbitOptions["Username"]}:{rabbitOptions["Password"]}@{rabbitOptions["Host"]}";
                hcBuilder.AddRabbitMQ(sp=> connectionFactory.CreateConnectionAsync(rabbitUri), name: "RabbitMQ");
            }

            return services;
        }

        public static void UseRiverHealthChecks(this IApplicationBuilder app)
        {
            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                // 使用 UI.Client 格式，方便 Dashboard 采集
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        }
    }
}