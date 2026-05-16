using System.Data;
using Dapper;
using RiverLi.DDD.Core.Application.Common.Models;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions;

    /// <summary>
    /// Dapper查询扩展方法
    /// </summary>
    public static class DapperQueryExtensions
    {
        /// <summary>
        /// 执行分页查询
        /// </summary> 
        public static async Task<PagedResult<T>> QueryPagedAsync<T>(
            this IDbConnection connection,
            string sql,
            PagedQuery pagedQuery,
            object? parameters = null,
            IDbTransaction? transaction = null)
        {
            pagedQuery.ValidateAndCorrect();

            // 计算总数
            var countSql = $"SELECT COUNT(*) FROM ({sql}) AS CountQuery";
            var totalCount = await connection.ExecuteScalarAsync<int>(
                countSql, parameters, transaction);

            // 分页查询
            var offset = (pagedQuery.PageIndex - 1) * pagedQuery.PageSize;
            var pagedSql = $"{sql} LIMIT {pagedQuery.PageSize} OFFSET {offset}";

            var items = await connection.QueryAsync<T>(
                pagedSql, parameters, transaction);

            return PagedResult<T>.SuccessResult(items,totalCount, pagedQuery.PageIndex, pagedQuery.PageSize);
        }

        /// <summary>
        /// 执行分页查询(MySQL语法)
        /// </summary>
        public static async Task<PagedResult<T>> QueryPagedMySqlAsync<T>(
            this IDbConnection connection,
            string selectSql,
            string fromWhereSql,
            PagedQuery pagedQuery,
            object? parameters = null,
            IDbTransaction? transaction = null)
        {
            pagedQuery.ValidateAndCorrect();

            // 计算总数
            var countSql = $"SELECT COUNT(*) {fromWhereSql}";
            var totalCount = await connection.ExecuteScalarAsync<int>(
                countSql, parameters, transaction);

            // 构建分页SQL
            var orderBy = string.IsNullOrWhiteSpace(pagedQuery.SortField)
                ? "ORDER BY Id DESC"
                : $"ORDER BY {pagedQuery.SortField}";

            var offset = (pagedQuery.PageIndex - 1) * pagedQuery.PageSize;
            var pagedSql = $"{selectSql} {fromWhereSql} {orderBy} LIMIT {pagedQuery.PageSize} OFFSET {offset}";

            var items = await connection.QueryAsync<T>(
                pagedSql, parameters, transaction);

            return PagedResult<T>.SuccessResult(items, totalCount, pagedQuery.PageIndex, pagedQuery.PageSize);
        }

        /// <summary>
        /// 执行软删除过滤查询
        /// </summary>
        public static async Task<IEnumerable<T>> QueryNotDeletedAsync<T>(
            this IDbConnection connection,
            string tableName,
            string? whereClause = null,
            object? parameters = null,
            IDbTransaction? transaction = null)
        {
            var sql = $"SELECT * FROM {tableName} WHERE IsDeleted = 0";

            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql += $" AND {whereClause}";
            }

            return await connection.QueryAsync<T>(sql, parameters, transaction);
        }

        /// <summary>
        /// 批量插入
        /// </summary>
        public static async Task<int> BulkInsertAsync<T>(
            this IDbConnection connection,
            string tableName,
            IEnumerable<T> entities,
            IDbTransaction? transaction = null)
        {
            if (!entities.Any()) return 0;

            var firstEntity = entities.First();
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanRead && p.Name != "Id")
                .ToList();

            var columns = string.Join(", ", properties.Select(p => p.Name));
            var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

            return await connection.ExecuteAsync(sql, entities, transaction);
        } 
    }