using System.Data;
using System.Linq.Expressions;
using Dapper;
using RiverLi.DDD.Core.Application.Common.Models;
using RiverLi.DDD.Core.Domain.Common;
using RiverLi.DDD.Core.Domain.Repositories;

namespace RiverLi.Blog.Infrastructure.Shared.Repositories;

/// <summary>
    /// Dapper仓储实现 - 完整的CRUD操作
    /// 注意: Dapper不支持IQueryable，部分高级查询功能需要自行实现
    /// </summary>
    public class DapperRepository<TAggregateRoot, TKey> : IRepository<TAggregateRoot, TKey>
        where TAggregateRoot : class, IAggregateRoot, IEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        protected readonly IDbConnection _connection;
        protected readonly IUnitOfWork _unitOfWork;
        protected readonly string _tableName;

        public DapperRepository(
            IDbConnection connection,
            IUnitOfWork unitOfWork,
            string? tableName = null)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _tableName = tableName ?? typeof(TAggregateRoot).Name + "s";
        }

        public IUnitOfWork UnitOfWork => _unitOfWork;

        #region IReadOnlyRepository 实现

        /// <summary>
        /// 注意: Dapper不支持IQueryable，此方法抛出异常
        /// 请使用专门的查询方法或使用EF Core
        /// </summary>
        public IQueryable<TAggregateRoot> AsQueryable()
        {
            throw new NotSupportedException(
                "Dapper不支持IQueryable。请使用专门的查询方法或切换到EF Core。");
        }

        public virtual async Task<PagedResult<TAggregateRoot>> GetPagedAsync(PagedQuery query, Expression<Func<TAggregateRoot, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        query.ValidateAndCorrect();

        // 构建 WHERE 子句和参数
        var (whereClause, parameters) = BuildWhereClause(predicate);

        var countSql = $"SELECT COUNT(*) FROM {_tableName}{whereClause}";
        var totalCount = await _connection.ExecuteScalarAsync<int>(countSql, parameters);

        if (totalCount == 0)
        {
            return PagedResult<TAggregateRoot>.Empty(query);
        }

        // 构建 ORDER BY 子句
        // 这里简化处理，可以根据需要扩展，例如从表达式中提取排序字段
        var orderByClause = "ORDER BY Id"; // 默认按 Id 排序

        var dataSql = $@"
            SELECT * FROM {_tableName}
            {whereClause}
            {orderByClause}
            LIMIT @Skip OFFSET @Take";

        // 添加分页参数
        parameters.Add("@Skip", (query.PageIndex - 1) * query.PageSize);
        parameters.Add("@Take", query.PageSize);

        var data = (await _connection.QueryAsync<TAggregateRoot>(dataSql, parameters)).ToList();

        return PagedResult<TAggregateRoot>.SuccessResult(data, totalCount, query.PageIndex, query.PageSize);
    }

    public virtual async Task<List<TAggregateRoot>> FindAsync(Expression<Func<TAggregateRoot, bool>> predicate, Expression<Func<TAggregateRoot, object>>? orderBy = null, bool ascending = true, CancellationToken cancellationToken = default)
    {
        // 构建 WHERE 子句和参数
        var (whereClause, parameters) = BuildWhereClause(predicate);

        // 构建 ORDER BY 子句
        var orderByClause = "";
        if (orderBy != null)
        {
            var orderProperty = GetPropertyName(orderBy);
            if (!string.IsNullOrEmpty(orderProperty))
            {
                orderByClause = $"ORDER BY {orderProperty} {(ascending ? "ASC" : "DESC")}";
            }
        }
        else
        {
             orderByClause = "ORDER BY Id"; // 默认排序
        }

        var sql = $@"
            SELECT * FROM {_tableName}
            {whereClause}
            {orderByClause}";

        var result = await _connection.QueryAsync<TAggregateRoot>(sql, parameters);
        return result.ToList();
        }

        public virtual async Task<TAggregateRoot?> GetByIdAsync(
            TKey id,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE Id = @Id";
            return await _connection.QueryFirstOrDefaultAsync<TAggregateRoot>(
                sql, new { Id = id });
        }

        public virtual async Task<List<TAggregateRoot>> GetByIdsAsync(
            IEnumerable<TKey> ids,
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT * FROM {_tableName} WHERE Id IN @Ids";
            var result = await _connection.QueryAsync<TAggregateRoot>(
                sql, new { Ids = ids });
            return result.ToList();
        }

        public virtual async Task<List<TAggregateRoot>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            var sql = $"SELECT * FROM {_tableName}";
            var result = await _connection.QueryAsync<TAggregateRoot>(sql);
            return result.ToList();
        }

        /// <summary>
        /// 注意: Dapper不支持Expression查询，此方法抛出异常
        /// </summary>
        public Task<List<TAggregateRoot>> FindAsync(
            Expression<Func<TAggregateRoot, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "Dapper不支持Expression查询。请使用带SQL参数的查询方法。");
        }

        /// <summary>
        /// 注意: Dapper不支持Expression查询，此方法抛出异常
        /// </summary>
        public Task<TAggregateRoot?> SingleOrDefaultAsync(
            Expression<Func<TAggregateRoot, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "Dapper不支持Expression查询。请使用带SQL参数的查询方法。");
        }

        /// <summary>
        /// 注意: Dapper不支持Expression查询，此方法抛出异常
        /// </summary>
        public Task<bool> ExistsAsync(
            Expression<Func<TAggregateRoot, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "Dapper不支持Expression查询。请使用带SQL参数的查询方法。");
        }

        public virtual async Task<long> CountAsync(
            Expression<Func<TAggregateRoot, bool>>? predicate = null,
            CancellationToken cancellationToken = default)
        {
            if (predicate != null)
            {
                throw new NotSupportedException(
                    "Dapper不支持Expression查询。请使用CountAsync()无参数版本或自定义SQL。");
            }

            var sql = $"SELECT COUNT(*) FROM {_tableName}";
            return await _connection.ExecuteScalarAsync<long>(sql);
        }

        #endregion

        #region IRepository 实现

        public virtual async Task<TAggregateRoot> AddAsync(
            TAggregateRoot aggregateRoot,
            CancellationToken cancellationToken = default)
        {
            var properties = GetProperties(aggregateRoot);
            var columns = string.Join(", ", properties.Keys);
            var values = string.Join(", ", properties.Keys.Select(k => $"@{k}"));

            var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({values})";
            await _connection.ExecuteAsync(sql, aggregateRoot);

            return aggregateRoot;
        }

        public virtual async Task AddRangeAsync(
            IEnumerable<TAggregateRoot> aggregateRoots,
            CancellationToken cancellationToken = default)
        {
            foreach (var aggregateRoot in aggregateRoots)
            {
                await AddAsync(aggregateRoot, cancellationToken);
            }
        }

        public virtual async Task<TAggregateRoot> UpdateAsync(
            TAggregateRoot aggregateRoot,
            CancellationToken cancellationToken = default)
        {
            var properties = GetProperties(aggregateRoot);
            var setClause = string.Join(", ", 
                properties.Keys.Where(k => k != "Id").Select(k => $"{k} = @{k}"));

            var sql = $"UPDATE {_tableName} SET {setClause} WHERE Id = @Id";
            await _connection.ExecuteAsync(sql, aggregateRoot);

            return aggregateRoot;
        }

        public virtual async Task UpdateRangeAsync(
            IEnumerable<TAggregateRoot> aggregateRoots,
            CancellationToken cancellationToken = default)
        {
            foreach (var aggregateRoot in aggregateRoots)
            {
                await UpdateAsync(aggregateRoot, cancellationToken);
            }
        }

        public virtual async Task DeleteAsync(
            TAggregateRoot aggregateRoot,
            CancellationToken cancellationToken = default)
        {
            var sql = $"DELETE FROM {_tableName} WHERE Id = @Id";
            await _connection.ExecuteAsync(sql, new { aggregateRoot.Id });
        }

        public virtual async Task DeleteByIdAsync(
            TKey id,
            CancellationToken cancellationToken = default)
        {
            var sql = $"DELETE FROM {_tableName} WHERE Id = @Id";
            await _connection.ExecuteAsync(sql, new { Id = id });
        }

        public virtual async Task DeleteRangeAsync(
            IEnumerable<TAggregateRoot> aggregateRoots,
            CancellationToken cancellationToken = default)
        {
            foreach (var aggregateRoot in aggregateRoots)
            {
                await DeleteAsync(aggregateRoot, cancellationToken);
            }
        }

        #endregion

        #region 辅助方法

    protected virtual Dictionary<string, object?> GetProperties(TAggregateRoot entity)
    {
        var properties = typeof(TAggregateRoot)
            .GetProperties()
            .Where(p => p.CanRead && p.CanWrite)
            .ToDictionary(
                p => p.Name,
                p => p.GetValue(entity)
            );

        return properties;
    }

    /// <summary>
    /// 根据 Expression<Func<T, bool>> 构建 WHERE 子句和 DynamicParameters
    /// </summary>
    /// <param name="predicate">过滤条件表达式</param>
    /// <returns>WHERE 子句 (包含 AND) 和 DynamicParameters</returns>
    private (string whereClause, DynamicParameters parameters) BuildWhereClause(Expression<Func<TAggregateRoot, bool>>? predicate)
    {
        var parameters = new DynamicParameters();
        var whereClause = "";

        if (predicate != null)
        {
            // 这是一个非常简化的实现，仅处理最基础的比较操作（==, !=, >, <, >=, <=）和 AND/OR
            // 复杂的表达式（如 Contains, StartsWith, 方法调用等）需要更复杂的解析逻辑
            var visitor = new ParameterizedSqlVisitor(parameters);
            var sqlFragment = visitor.Visit(predicate.Body);

            if (!string.IsNullOrWhiteSpace(sqlFragment))
            {
                whereClause = " WHERE " + sqlFragment;
            }
        }

        return (whereClause, parameters);
    }

    /// <summary>
    /// 从 Expression<Func<T, object>> 中提取属性名
    /// </summary>
    /// <param name="expression">表达式</param>
    /// <returns>属性名</returns>
    private static string? GetPropertyName<T>(Expression<Func<T, object>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        else if (expression.Body is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
        {
            if (unaryExpression.Operand is MemberExpression operandMemberExpression)
            {
                return operandMemberExpression.Member.Name;
            }
        }
        return null; // 或抛出异常，取决于你的需求
    }

    #endregion
    }

    /// <summary>
    /// Guid主键的便捷仓储
    /// </summary>
    public class DapperRepository<TAggregateRoot> : DapperRepository<TAggregateRoot, Guid>
        where TAggregateRoot : class, IAggregateRoot, IEntity<Guid>
    {
        public DapperRepository(
            IDbConnection connection,
            IUnitOfWork unitOfWork,
            string? tableName = null)
            : base(connection, unitOfWork, tableName)
        {
        }
    }