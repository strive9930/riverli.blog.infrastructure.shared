// RiverLi.Blog.Infrastructure.Shared/Security/IApiPermissionChecker.cs
using System;
using System.Threading.Tasks;

namespace RiverLi.Blog.Infrastructure.Shared.Security
{
    public interface IApiPermissionChecker
    {
        /// <summary>
        /// 校验用户是否有权访问指定的 API
        /// </summary>
        Task<bool> IsGrantedAsync(Guid userId, string method, string routeTemplate);

        Task<bool> HasApiPermissionAsync(Guid userId, string routePattern, string httpMethod);
    }
}