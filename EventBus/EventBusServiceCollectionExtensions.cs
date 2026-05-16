using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RiverLi.DDD.Core.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiverLi.Blog.Infrastructure.Shared.EventBus
{
    public static class EventBusServiceCollectionExtensions
    {
        /// <summary>
        /// 注册 River 消息总线 (RabbitMQ + MassTransit)
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <param name="configureConsumers">用于注册消费者的回调</param>
        /// <returns></returns>
        public static IServiceCollection AddRiverEventBus(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<IBusRegistrationConfigurator>? configureConsumers = null)
        {
            // 1. 注册发布者实现 (DI)
            // 这样你在代码里注入 IDomainEventPublisher 时，拿到的就是 MassTransit 的实现
            services.AddScoped<IDomainEventPublisher, MassTransitEventPublisher>();

            // 2. 配置 MassTransit
            services.AddMassTransit(x =>
            {
                // 允许外部微服务注册自己的消费者 (Consumers)
                configureConsumers?.Invoke(x);

                // 使用 RabbitMQ 作为传输层
                x.UsingRabbitMq((context, cfg) =>
                {
                    // 读取配置
                    var options = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>();

                    if (options == null)
                    {
                        // 默认配置（如果配置文件没写）
                        options = new RabbitMqOptions();
                    }

                    cfg.Host(options.Host, options.VirtualHost, h =>
                    {
                        h.Username(options.Username);
                        h.Password(options.Password);
                    });

                    // 自动配置端点 (Auto Config Endpoints)
                    // 这一步非常方便，它会自动根据 Consumer 的名字创建 Queue
                    // 例如：ArticleCreatedConsumer -> ArticleCreated 队列
                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }
    }
}
