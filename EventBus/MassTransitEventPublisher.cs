using MassTransit;
using RiverLi.DDD.Core.Domain.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiverLi.Blog.Infrastructure.Shared.EventBus
{
    /// <summary>
    /// 基于 MassTransit 的领域事件发布者实现
    /// </summary>
    public class MassTransitEventPublisher : IDomainEventPublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public MassTransitEventPublisher(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            // MassTransit 的 Publish 会自动根据消息类型（domainEvent的实际类型）路由到对应的 Exchange
            await _publishEndpoint.Publish((object)domainEvent, cancellationToken);
        }

        public async Task PublishBatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            // 批量发布
            await _publishEndpoint.PublishBatch(domainEvents.Cast<object>(), cancellationToken);
        }
    }
}
