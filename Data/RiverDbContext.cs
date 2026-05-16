using System.Linq.Expressions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RiverLi.DDD.Core.Application.Common.Interfaces;
using RiverLi.DDD.Core.Domain.Common;
using RiverLi.DDD.Core.Domain.Events;
using RiverLi.DDD.Core.Domain.Repositories;
using System.Reflection;
using System.Security.Principal;

namespace RiverLi.Blog.Infrastructure.Shared.Data
{
    public abstract class RiverDbContext : DbContext, IUnitOfWork
    {
        private readonly IMediator _mediator;
        private readonly ICurrentUser _currentUser;

        protected RiverDbContext(
            DbContextOptions options,
            IMediator mediator,
            ICurrentUser currentUser) : base(options)
        {
            _mediator = mediator;
            _currentUser = currentUser;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // 为所有实现ISoftDelete的实体添加全局过滤器
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var body = Expression.Equal(
                        Expression.Property(parameter, nameof(ISoftDelete.IsDeleted)),
                        Expression.Constant(false)
                    );
                    var lambda = Expression.Lambda(body, parameter);
            
                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }    
            // 自动加载当前程序集中的所有 EntityConfiguration (实现 IEntityTypeConfiguration 的类)
            modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
        }

        public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            // 1. 在提交事务之前，分发领域事件 (Domain Events)
            // 这是一个常用的 DDD 模式：先处理副作用，再持久化
            await DispatchDomainEventsAsync();

            // 2. 自动填充审计字段 和 处理软删除
            HandleAuditingAndSoftDelete();

            // 3. 提交数据库
            var result = await base.SaveChangesAsync(cancellationToken);

            return result > 0;
        }

        private void HandleAuditingAndSoftDelete()
        {
            var userId = _currentUser.Id; // 获取当前操作人ID

            // 获取所有实现了 IAuditableEntity 的变更条目
            var auditableEntries = ChangeTracker.Entries<IAuditableEntity>();

            foreach (var entry in auditableEntries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreateTime = DateTime.UtcNow;
                        entry.Entity.Creator = userId;
                        // 防止新增时 UpdateTime 有值
                        entry.Entity.UpdateTime = null;
                        entry.Entity.Updator = null;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdateTime = DateTime.UtcNow;
                        entry.Entity.Updator = userId;
                        // 保护创建时间不被修改
                        entry.Property(x => x.CreateTime).IsModified = false;
                        entry.Property(x => x.Creator).IsModified = false;
                        break;
                }
            }

            // 处理软删除 (拦截 Deleted 状态)
            var deletedEntries = ChangeTracker.Entries<ISoftDelete>().Where(e => e.State == EntityState.Deleted);

            foreach (var entry in deletedEntries)
            {
                // 1. 将状态改为 Modified，避免被物理删除
                entry.State = EntityState.Modified;

                // 2. 设置软删除标记
                entry.Entity.IsDeleted = true;
                entry.Entity.DeleteTime = DateTime.UtcNow;

                // 3. 如果实体同时也实现了审计接口，更新 更新时间/更新人
                if (entry.Entity is IAuditableEntity auditable)
                {
                    auditable.UpdateTime = DateTime.UtcNow;
                    auditable.Updator = userId;
                }
            }
        }

        private async Task DispatchDomainEventsAsync()
        {
            // 获取所有包含未发布领域事件的实体
            var domainEntities = ChangeTracker
            .Entries<BaseEntity>() // 假设 IEntity 包含了 DomainEvents 集合，或者你需要定义一个包含 DomainEvents 的接口
                .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any());

            // 注意：如果在 BaseEntity 中定义了 DomainEvents，需确保 BaseEntity 暴露了该集合
            // 这里假设 BaseEntity 实现了相关逻辑。如果 BaseEntity 没有 DomainEvents 属性，
            // 你需要在 BaseEntity 中添加 `private List<IDomainEvent> _domainEvents;` 和相关方法

            var domainEvents = domainEntities
                .SelectMany(x => x.Entity.DomainEvents)
                .ToList();

            domainEntities.ToList()
                .ForEach(entity => entity.Entity.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
            {
                await _mediator.Publish(domainEvent);
            }
        }
    }

}
