/*
# RiverLi.Infrastructure.Dapper

Dapper轻量级ORM实现，适用于对性能要求高、不需要复杂查询的场景。

## 特点

✅ 高性能: Dapper是最快的.NET ORM之一
✅ 轻量级: 代码简洁，学习成本低
✅ 灵活性: 完全控制SQL，适合复杂查询

❌ 不支持IQueryable: 无法使用LINQ动态查询
❌ 不支持Expression: 需要手写SQL
❌ 无变更追踪: 需要手动管理实体状态

## 使用场景

- 高性能读写场景
- 复杂SQL查询
- 存储过程调用
- 批量数据处理

## 不适用场景

- 需要动态LINQ查询
- 需要Expression Tree
- 需要复杂的ORM特性(如延迟加载、变更追踪)

如果需要以上特性，请使用 RiverLi.Infrastructure.EfCore

## 使用示例

```csharp
// Program.cs
builder.Services.AddRiverLiDapperMySql(
    builder.Configuration.GetConnectionString("DefaultConnection"));

// 自定义查询
public class TagRepository : DapperRepository<Tag>
{
    public TagRepository(IDbConnection connection, IUnitOfWork unitOfWork)
        : base(connection, unitOfWork, "Tags")
    {
    }

    public async Task<List<Tag>> GetPopularTagsAsync(int count)
    {
        var sql = @"
            SELECT * FROM Tags 
            WHERE IsDeleted = 0 
            ORDER BY ArticleCount DESC 
            LIMIT @Count";

        var result = await _connection.QueryAsync<Tag>(sql, new { Count = count });
        return result.ToList();
    }

    public async Task<PagedResult<Tag>> SearchTagsAsync(string keyword, PagedQuery pagedQuery)
    {
        var selectSql = "SELECT *";
        var fromWhereSql = @"
            FROM Tags 
            WHERE IsDeleted = 0 
            AND (Name LIKE @Keyword OR Description LIKE @Keyword)";

        return await _connection.QueryPagedMySqlAsync<Tag>(
            selectSql,
            fromWhereSql,
            pagedQuery,
            new { Keyword = $"%{keyword}%" }
        );
    }
}
```

## 事务管理

```csharp
// 手动事务管理
var unitOfWork = _serviceProvider.GetRequiredService<IUnitOfWork>();
var dapperUoW = (DapperUnitOfWork)unitOfWork;

dapperUoW.BeginTransaction();

try
{
    await _tagRepository.AddAsync(tag);
    await _categoryRepository.AddAsync(category);
    
    await unitOfWork.SaveEntitiesAsync();
}
catch
{
    // 事务会自动回滚
    throw;
}
```
*/