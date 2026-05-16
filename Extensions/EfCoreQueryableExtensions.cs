using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RiverLi.DDD.Core.Application.Common.Models;
using RiverLi.DDD.Core.Domain.Common;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions;
/// <summary>
/// IQueryable扩展方法 - 配合DDD.Core使用
/// </summary>
public static class EfCoreQueryableExtensions
{
    /// <summary>
        /// 转换为分页结果(使用DDD.Core的PagedResult和PagedQuery)
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            PagedQuery pagedQuery,
            CancellationToken cancellationToken = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (pagedQuery == null) throw new ArgumentNullException(nameof(pagedQuery));

            pagedQuery.ValidateAndCorrect();

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((pagedQuery.PageIndex - 1) * pagedQuery.PageSize)
                .Take(pagedQuery.PageSize)
                .ToListAsync(cancellationToken);

            return PagedResult<T>.SuccessResult(items, totalCount, pagedQuery.PageIndex, pagedQuery.PageSize);
        }

        /// <summary>
        /// 条件过滤扩展
        /// </summary>
        public static IQueryable<T> WhereIf<T>(
            this IQueryable<T> query,
            bool condition,
            Expression<Func<T, bool>> predicate)
        {
            return condition ? query.Where(predicate) : query;
        }

        /// <summary>
        /// 软删除过滤(配合DDD.Core的ISoftDelete)
        /// </summary>
        public static IQueryable<T> WhereNotDeleted<T>(this IQueryable<T> query)
            where T : class, ISoftDelete
        {
            return query.Where(x => !x.IsDeleted);
        }

        /// <summary>
        /// 包含已删除数据(忽略全局过滤器)
        /// </summary>
        public static IQueryable<T> IncludeDeleted<T>(this IQueryable<T> query)
            where T : class  // ✅ 添加class约束
        {
            return query.IgnoreQueryFilters();
        }

        /// <summary>
        /// 动态排序(支持格式: "CreateTime desc" 或 "Name")
        /// </summary>
        public static IQueryable<T> ApplySort<T>(
            this IQueryable<T> query,
            string? sortField)
        {
            if (string.IsNullOrWhiteSpace(sortField))
                return query;

            var parts = sortField.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var propertyName = parts[0];
            var descending = parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase);

            try
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(parameter, propertyName);
                var lambda = Expression.Lambda(property, parameter);

                var methodName = descending ? "OrderByDescending" : "OrderBy";
                var resultExpression = Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new Type[] { typeof(T), property.Type },
                    query.Expression,
                    Expression.Quote(lambda));

                return query.Provider.CreateQuery<T>(resultExpression);
            }
            catch (ArgumentException)
            {
                // 属性不存在,返回原查询
                return query;
            }
        }

        /// <summary>
        /// 搜索扩展(多字段模糊匹配)
        /// 示例: query.Search("关键词", x => x.Title, x => x.Content)
        /// </summary>
        public static IQueryable<T> Search<T>(
            this IQueryable<T> query,
            string? searchTerm,
            params Expression<Func<T, string>>[] properties)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || properties == null || properties.Length == 0)
                return query;

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression? predicate = null;

            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;

            foreach (var propertyExpr in properties)
            {
                var property = Expression.Invoke(propertyExpr, parameter);
                
                // 添加null检查
                var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
                var constant = Expression.Constant(searchTerm);
                var containsCall = Expression.Call(property, containsMethod, constant);
                var condition = Expression.AndAlso(nullCheck, containsCall);

                predicate = predicate == null
                    ? condition
                    : Expression.OrElse(predicate, condition);
            }

            if (predicate != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(predicate, parameter);
                query = query.Where(lambda);
            }

            return query;
        }

        /// <summary>
        /// 日期范围过滤
        /// </summary>
        public static IQueryable<T> WhereDateBetween<T>(
            this IQueryable<T> query,
            Expression<Func<T, DateTime>> dateSelector,
            DateTime? startDate,
            DateTime? endDate)
        {
            if (startDate.HasValue)
            {
                var parameter = dateSelector.Parameters[0];
                var body = Expression.GreaterThanOrEqual(
                    dateSelector.Body,
                    Expression.Constant(startDate.Value));
                var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
                query = query.Where(lambda);
            }

            if (endDate.HasValue)
            {
                var parameter = dateSelector.Parameters[0];
                var body = Expression.LessThanOrEqual(
                    dateSelector.Body,
                    Expression.Constant(endDate.Value));
                var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
                query = query.Where(lambda);
            }

            return query;
        }

        /// <summary>
        /// 批量更新扩展 (EF Core 7+)
        /// </summary>
        public static async Task<int> BatchUpdateAsync<T>(
            this IQueryable<T> query,
            Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<T>, 
                Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<T>>> setPropertyCalls,
            CancellationToken cancellationToken = default)
            where T : class
        {
            return await query.ExecuteUpdateAsync(setPropertyCalls, cancellationToken);
        }

        /// <summary>
        /// 批量删除扩展(EF Core 7+)
        /// </summary>
        public static async Task<int> BatchDeleteAsync<T>(
            this IQueryable<T> query,
            CancellationToken cancellationToken = default)
            where T : class
        {
            return await query.ExecuteDeleteAsync(cancellationToken);
        }

        /// <summary>
        /// NoTracking查询(只读场景优化)
        /// </summary>
        public static IQueryable<T> AsNoTrackingQuery<T>(this IQueryable<T> query)
            where T : class
        {
            return query.AsNoTracking();
        }
}