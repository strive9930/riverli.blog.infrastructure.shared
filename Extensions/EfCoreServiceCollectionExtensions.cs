using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RiverLi.Blog.Infrastructure.Shared.Repositories;
using RiverLi.DDD.Core.Domain.Repositories;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions;
/// <summary>
/// 服务注册扩展
/// </summary>
public static class EfCoreServiceCollectionExtensions
{
    /// <summary>
    /// 添加EF Core仓储支持
    /// </summary>
    public static IServiceCollection AddRiverLiEfCore(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? optionsAction = null)
    {
        // 注册泛型仓储
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(EfRepository<,>));

        // 注册Guid主键的便捷仓储
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.AddScoped(typeof(IReadOnlyRepository<,>), typeof(EfRepository<,>));

        return services;
    }

    /// <summary>
    /// 添加EF Core仓储支持(带DbContext)
    /// </summary>
    public static IServiceCollection AddRiverLiEfCore<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
        where TContext : DbContext
    {
        // 注册DbContext
        services.AddDbContext<TContext>(optionsAction);

        // 注册仓储
        services.AddRiverLiEfCore();

        return services;
    }
}