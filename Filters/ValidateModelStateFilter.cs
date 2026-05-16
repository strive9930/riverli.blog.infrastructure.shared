using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RiverLi.DDD.Core.Application.Common.Models;

namespace RiverLi.Blog.Infrastructure.Shared.Filters
{
    /// <summary>
    /// 模型验证过滤器 - 自动处理模型验证失败的情况
    /// </summary>
    public class ValidateModelStateFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var result = ApiResult.FailResult("验证失败", 400);
                result.Errors = errors;

                context.Result = new BadRequestObjectResult(result);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // 不需要在执行后处理
        }
    }
}