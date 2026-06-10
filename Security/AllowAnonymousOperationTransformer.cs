using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace RiverLi.Blog.Infrastructure.Shared.Security;

public class AllowAnonymousOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        // 极其优雅地从官方上下文中读取元数据
        var hasAnonymous = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();

        if (hasAnonymous)
        {
            // 往 OpenAPI JSON 中注入我们自定义的扩展标识
            operation.Extensions["x-allow-anonymous"] = new Microsoft.OpenApi.Any.OpenApiBoolean(true);
        }

        return Task.CompletedTask;
    }
}