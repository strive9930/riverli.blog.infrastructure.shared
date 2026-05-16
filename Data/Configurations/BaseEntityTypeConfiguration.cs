using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RiverLi.DDD.Core.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiverLi.Blog.Infrastructure.Shared.Data.Configurations
{
    /// <summary>
    /// 实体配置基类（支持自定义主键类型）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TKey">主键类型</typeparam>
    public abstract class BaseEntityTypeConfiguration<TEntity, TKey> : IEntityTypeConfiguration<TEntity>
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            // 1. 统一配置主键
            builder.HasKey(x => x.Id);

            // 2. 全局过滤：查询时自动过滤掉 IsDeleted = true 的数据 (软删除核心)
            builder.HasQueryFilter(x => !x.IsDeleted);

            // 3. 基础字段配置
            // 审计字段通常不需要 max，但为了数据库性能，建议设置合理长度
            builder.Property(x => x.Creator).HasMaxLength(64).IsRequired(false);
            builder.Property(x => x.Updator).HasMaxLength(64).IsRequired(false);

            // 软删除字段
            builder.Property(x => x.IsDeleted).HasDefaultValue(false);

            // 4. 忽略领域事件 (非常重要！)
            // DomainEvents 是内存属性，不应该映射到数据库表中
            builder.Ignore(x => x.DomainEvents);

            // 5. 调用子类的自定义配置
            ConfigureEntity(builder);
        }

        /// <summary>
        /// 子类必须实现的具体配置方法
        /// </summary>
        public abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
    }

    /// <summary>
    /// 实体配置基类（默认 Guid 主键，适配 BaseEntity）
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    public abstract class BaseEntityTypeConfiguration<TEntity> : BaseEntityTypeConfiguration<TEntity, Guid>
        where TEntity : BaseEntity
    {
    }
}
