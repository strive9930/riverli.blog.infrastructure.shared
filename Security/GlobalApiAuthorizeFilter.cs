// RiverLi.Blog.Infrastructure.Shared/Security/GlobalApiAuthorizeFilter.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace RiverLi.Blog.Infrastructure.Shared.Security
{
    public class GlobalApiAuthorizeFilter : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // 1. 检查端点是否挂了 [AllowAnonymous] 免死金牌
            var endpoint = context.HttpContext.GetEndpoint();
            var isAnonymous = endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null;
    
            if (isAnonymous)
            {
                return; // 遇到官方 [AllowAnonymous]，直接放行！
            }

            // 2. 检查是否登录 (必须有 Token)
            var user = context.HttpContext.User;
            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult(); // 401
                return;
            }

            // 3. 提取 UserId
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // 🌟 4. 提取当前请求的方法和路由模板
            // 注意：不要用 Request.Path (那会拿到 /api/users/123)
            // 用 AttributeRouteInfo.Template 会拿到干净的规则： "api/identity/users/{id}"
            var method = context.HttpContext.Request.Method.ToUpper();
            var routeTemplate = context.ActionDescriptor.AttributeRouteInfo?.Template;

            if (string.IsNullOrEmpty(routeTemplate))
            {
                // 如果接口没有写路由规范，出于安全考虑直接拒绝
                context.Result = new ForbidResult(); 
                return;
            }

            // 统一路由前缀格式，方便数据库匹配 (例如强制加 /)
            routeTemplate = routeTemplate.StartsWith("/") ? routeTemplate : "/" + routeTemplate;

            // 5. 调用业务侧实现的查询器进行比对
            var permissionChecker = context.HttpContext.RequestServices.GetService<IApiPermissionChecker>();
            if (permissionChecker == null)
            {
                context.Result = new StatusCodeResult(500); 
                return;
            }

            var isGranted = await permissionChecker.IsGrantedAsync(userId, method, routeTemplate);
            if (!isGranted)
            {
                context.Result = new ForbidResult(); // 403
            }
        }
    }
}