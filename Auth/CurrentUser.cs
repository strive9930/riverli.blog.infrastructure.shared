using System.ComponentModel;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using RiverLi.DDD.Core.Application.Common.Interfaces; // 你的 ICurrentUser 命名空间

namespace RiverLi.Blog.Infrastructure.Shared.Auth
{
    public class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public T? GetUserId<T>()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)
                              ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub"); // JWT 标准通常用 sub

            if (userIdClaim == null || string.IsNullOrEmpty(userIdClaim.Value))
            {
                return default;
            }

            // 尝试将字符串 ID 转换为目标类型 T (例如 Guid 或 int)
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                return (T?)converter.ConvertFromInvariantString(userIdClaim.Value);
            }
            catch
            {
                return default;
            }
        }

        public bool IsInRole(string role) => _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public string? Id
        {
            get
            {
                var idClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier) 
                              ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub");

                if (idClaim != null && !string.IsNullOrEmpty(idClaim.Value))
                {
                    if (!string.IsNullOrEmpty(idClaim.Value))
                    {
                        var userId = idClaim.Value;
                        return userId;
                    }
                    else
                    {
                        // 记录日志：无效的用户 ID 格式
                        Console.WriteLine($"Invalid user ID format: {idClaim.Value}");
                    }
                }

                return null; // 或返回默认值/抛出异常
            }
        }
        
        //public string? UserName => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;
        public string? UserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
        public IEnumerable<Claim>? Claims => _httpContextAccessor.HttpContext?.User?.Claims;
    }
}