using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RiverLi.DDD.Core.Domain.Common;
using RiverLi.DDD.Core.Domain.Repositories;
using RiverLi.Blog.Infrastructure.Shared.Data;
using RiverLi.DDD.Core.Application.Common.Models;

namespace RiverLi.Blog.Infrastructure.Shared.Repositories
{
    /// <summary>
    /// EF Core 通用仓储实现（适配双泛型）
    /// </summary>
    /// <typeparam name="TAggregateRoot">聚合根类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    public class EfRepository<TAggregateRoot, TKey> : IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot, IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        protected readonly RiverDbContext _context;
        protected readonly DbSet<TAggregateRoot> _dbSet;

        public EfRepository(RiverDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<TAggregateRoot>();
        }

        // 实现 IRepository 中的 UnitOfWork 属性
        public IUnitOfWork UnitOfWork => _context;

        #region Write Operations (IRepository Implementation)

        public virtual async Task<TAggregateRoot> AddAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            var entry = await _dbSet.AddAsync(aggregateRoot, cancellationToken);
            return entry.Entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<TAggregateRoot> aggregateRoots, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddRangeAsync(aggregateRoots, cancellationToken);
        }

        public virtual Task<TAggregateRoot> UpdateAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            // 显式标记为 Modified，确保 EF Core 追踪到变更
            var entry = _dbSet.Update(aggregateRoot);
            return Task.FromResult(entry.Entity);
        }

        public virtual Task UpdateRangeAsync(IEnumerable<TAggregateRoot> aggregateRoots, CancellationToken cancellationToken = default)
        {
            _dbSet.UpdateRange(aggregateRoots);
            return Task.CompletedTask;
        }

        public virtual Task DeleteAsync(TAggregateRoot aggregateRoot, CancellationToken cancellationToken = default)
        {
            // RiverDbContext 会拦截 Deleted 状态并转换为软删除（如果实体实现了 ISoftDelete）
            _dbSet.Remove(aggregateRoot);
            return Task.CompletedTask;
        }

        public virtual Task DeleteRangeAsync(IEnumerable<TAggregateRoot> aggregateRoots, CancellationToken cancellationToken = default)
        {
            _dbSet.RemoveRange(aggregateRoots);
            return Task.CompletedTask;
        }

        public virtual async Task DeleteByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            // 先查询后删除，利用 Context 缓存
            var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);

            if (entity != null)
            {
                await DeleteAsync(entity, cancellationToken);
            }
        }

        #endregion

        #region Read Operations (IReadOnlyRepository Implementation)

        public virtual async Task<TAggregateRoot?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        }

        public virtual async Task<List<TAggregateRoot>> GetByIdsAsync(IEnumerable<TKey> ids, CancellationToken cancellationToken = default)
        {
            // 注意：EF Core 翻译 Contains 需要 ids 不为空
            if (ids == null || !ids.Any())
            {
                return new List<TAggregateRoot>();
            }

            return await _dbSet.Where(x => ids.Contains(x.Id)).ToListAsync(cancellationToken);
        }

        public virtual async Task<List<TAggregateRoot>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }

        public virtual async Task<List<TAggregateRoot>> FindAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
        }

        public virtual async Task<TAggregateRoot?> SingleOrDefaultAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.SingleOrDefaultAsync(predicate, cancellationToken);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<TAggregateRoot, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(predicate, cancellationToken);
        }

        public virtual async Task<long> CountAsync(Expression<Func<TAggregateRoot, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            if (predicate == null)
            {
                return await _dbSet.LongCountAsync(cancellationToken);
            }
            return await _dbSet.LongCountAsync(predicate, cancellationToken);
        }

        public virtual IQueryable<TAggregateRoot> AsQueryable()
        {
            // 直接返回 DbSet 供上层自由组合（如分页、排序、Include导航属性）
            // 如果需要无追踪查询，调用方可以链式调用 .AsNoTracking()
            return _dbSet.AsNoTracking();
        }

        public virtual async Task<PagedResult<TAggregateRoot>> GetPagedAsync(
            PagedQuery query,
            Expression<Func<TAggregateRoot, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            query.ValidateAndCorrect();

            IQueryable<TAggregateRoot> queryable = _dbSet;

            if (predicate != null)
            {
                queryable = queryable.Where(predicate);
            }

            // 计算总数
            var totalCount = await queryable.CountAsync(cancellationToken);

            if (totalCount == 0)
            {
                return PagedResult<TAggregateRoot>.Empty(query);
            }

            // 默认排序，可以扩展为从 query 或其他地方获取排序信息
            // 这里假设实体有 Id 属性，如果没有，需要传入默认排序字段
            // 或者要求调用方总是提供 orderBy 表达式
            // 为了简单起见，这里默认按 Id 排序
            // 也可以尝试从 query 对象中解析排序信息（如果 PagedQuery 支持）
            // 例如：var orderedQuery = ApplyOrdering(queryable, query);
            // 但这里我们先用一个简化的默认排序
            queryable = queryable.OrderBy(e => e.Id); // 假设实体有 Id 属性

            // 分页
            var data = await queryable
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<TAggregateRoot>.SuccessResult(data, totalCount, query.PageIndex, query.PageSize);
        }

        public async Task<List<TAggregateRoot>> FindAsync(Expression<Func<TAggregateRoot, bool>> predicate, Expression<Func<TAggregateRoot, object>>? orderBy = null, bool ascending = true,
            CancellationToken cancellationToken = default)
        {
            return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
        }

        #endregion
        #region Helper Methods (可选：用于更复杂的分页排序逻辑)

        /// <summary>
        /// 根据 PagedQuery 中的排序信息动态应用排序
        /// 注意：这需要 PagedQuery 包含排序字段和方向的信息
        /// 这是一个示例实现，需要根据 PagedQuery 的实际结构进行调整
        /// </summary>
        // private IQueryable<TAggregateRoot> ApplyOrdering(IQueryable<TAggregateRoot> queryable, PagedQuery query)
        // {
        //     // 假设 PagedQuery 有 OrderByProperty 和 SortDirection 属性
        //     if (string.IsNullOrEmpty(query.OrderByProperty)) // 或其他判断条件
        //     {
        //         // 如果没有指定排序，则按 Id 排序
        //         return queryable.OrderBy(e => e.Id);
        //     }
        //
        //     // 使用反射和 Expression Trees 动态构建排序表达式
        //     // 这比手动写多个 if/else 更灵活
        //     var parameter = Expression.Parameter(typeof(TAggregateRoot), "x");
        //     var property = Expression.Property(parameter, query.OrderByProperty);
        //     var lambda = Expression.Lambda(property, parameter);
        //
        //     var methodName = query.SortDirection?.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";
        //     var method = typeof(Queryable).GetMethods()
        //         .First(m => m.Name == methodName && m.GetParameters().Length == 2)
        //         .MakeGenericMethod(typeof(TAggregateRoot), property.Type);
        //
        //     return (IQueryable<TAggregateRoot>)method.Invoke(null, new object[] { queryable, lambda });
        // }

        #endregion
    }
}
