using MediatR;
using Microsoft.EntityFrameworkCore;
using RiverLi.DDD.Core.Application.Common.Interfaces;
using RiverLi.DDD.Core.Domain.Common;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions;
/// <summary>
/// DbContext扩展方法 - 支持UnitOfWork模式
/// </summary>
public static class EfCoreDbContextExtensions
{
    /// <summary>
        /// 分发领域事件(使用MediatR)
        /// </summary>
        public static async Task DispatchDomainEventsAsync(
            this DbContext context,
            IMediator mediator,
            CancellationToken cancellationToken = default)
        {
            if (mediator == null) return;

            var domainEntities = context.ChangeTracker
                .Entries<BaseEntity>()
                .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
                .Select(x => x.Entity)
                .ToList();

            var domainEvents = domainEntities
                .SelectMany(x => x.DomainEvents!)
                .ToList();

            // 清空领域事件
            domainEntities.ForEach(entity => entity.ClearDomainEvents());

            // 发布领域事件
            foreach (var domainEvent in domainEvents)
            {
                await mediator.Publish(domainEvent, cancellationToken);
            }
        }

        /// <summary>
        /// 设置审计字段(创建人、更新人等)
        /// </summary>
        public static void SetAuditFields(
            this DbContext context,
            ICurrentUser? currentUser)
        {
            var entries = context.ChangeTracker
                .Entries<BaseEntity>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.Creator = currentUser?.Id;
                    entry.Entity.CreateTime = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.Updator = currentUser?.Id;
                    entry.Entity.UpdateModifyTime();
                }
            }
        }

        /// <summary>
        /// 设置软删除字段
        /// </summary>
        public static void SetSoftDeleteFields(
            this DbContext context,
            ICurrentUser? currentUser)
        {
            var entries = context.ChangeTracker
                .Entries<ISoftDelete>()
                .Where(e => e.State == EntityState.Deleted);

            foreach (var entry in entries)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeleteTime = DateTime.UtcNow;

                // 如果实体同时实现了IAuditableEntity
                if (entry.Entity is IAuditableEntity auditableEntity)
                {
                    auditableEntity.UpdateTime = DateTime.UtcNow;
                    auditableEntity.Updator = currentUser?.Id;
                }
            }
        }
}