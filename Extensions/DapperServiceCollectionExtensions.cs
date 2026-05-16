using System.Data;
using Microsoft.Extensions.DependencyInjection;
using RiverLi.Blog.Infrastructure.Shared.Data;
using RiverLi.Blog.Infrastructure.Shared.Repositories;
using RiverLi.DDD.Core.Domain.Repositories;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions;

/// <summary>
/// 服务注册扩展
/// </summary>
public static class DapperServiceCollectionExtensions
{
    /// <summary>
    /// 添加Dapper仓储支持
    /// </summary>
    public static IServiceCollection AddRiverLiDapper(
        this IServiceCollection services,
        Func<IServiceProvider, IDbConnection> connectionFactory)
    {
        // 注册IDbConnection
        services.AddScoped(connectionFactory);

        // 注册UnitOfWork
        services.AddScoped<IUnitOfWork, DapperUnitOfWork>();

        // 注册泛型仓储
        services.AddScoped(typeof(IRepository<,>), typeof(DapperRepository<,>));
        services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(DapperRepository<,>));

        // 注册Guid主键的便捷仓储
        services.AddScoped(typeof(IRepository<,>), typeof(DapperRepository<,>));
        services.AddScoped(typeof(IReadOnlyRepository<>), typeof(DapperRepository<>));

        return services;
    }

    /// <summary>
    /// 添加Dapper仓储支持(MySQL)
    /// </summary>
    public static IServiceCollection AddRiverLiDapperMySql(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddRiverLiDapper(sp =>
        {
            var connection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            connection.Open();
            return connection;
        });
    }
}