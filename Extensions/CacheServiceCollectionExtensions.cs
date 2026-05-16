using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RiverLi.DDD.Core.Application.Common.Interfaces;
using RiverLi.Blog.Infrastructure.Shared.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions
{
    public static class CacheServiceCollectionExtensions
    {
        public static IServiceCollection AddRiverRedisCache(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. 注册原生 Redis 分布式缓存
            services.AddStackExchangeRedisCache(options =>
            {
                // 从配置文件读取连接字符串 "Redis:ConnectionString"
                options.Configuration = configuration.GetSection("Redis:ConnectionString").Value ?? "localhost:6379";
                options.InstanceName = "RiverBlog_"; // Key 前缀，防止冲突
            });

            // 2. 注册我们的统一封装接口
            services.AddScoped<ICacheService, RedisCacheService>();

            return services;
        }
    }
}
