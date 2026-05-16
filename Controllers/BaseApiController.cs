using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RiverLi.DDD.Core.Application.Common.Interfaces;
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using RiverLi.DDD.Core.Application.Common.Models;

namespace RiverLi.Blog.Infrastructure.Shared.Controllers
{
    /// <summary>
    /// API控制器基类，提供统一的响应格式、异常处理和常用功能
    /// </summary>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        private IMediator? _mediator;
        private ICurrentUser? _currentUser;
        private ILogger? _logger;

        /// <summary>
        /// MediatR中介者模式实例
        /// </summary>
        protected IMediator Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

        /// <summary>
        /// 当前用户信息
        /// </summary>
        protected ICurrentUser CurrentUser => _currentUser ??= HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

        /// <summary>
        /// 日志记录器
        /// </summary>
        protected ILogger Logger => _logger ??= HttpContext.RequestServices.GetRequiredService<ILogger<BaseApiController>>();

        #region 当前用户快捷属性

        /// <summary>
        /// 当前用户ID（Guid类型）
        /// </summary>
        protected Guid? UserId => CurrentUser.GetUserId<Guid>();

        /// <summary>
        /// 当前用户ID（字符串类型）
        /// </summary>
        protected string? UserIdString => CurrentUser.Id;

        /// <summary>
        /// 当前用户名
        /// </summary>
        protected string? UserName => CurrentUser.UserName;

        /// <summary>
        /// 当前用户邮箱
        /// </summary>
        protected string? UserEmail => CurrentUser.Email;

        /// <summary>
        /// 是否已认证
        /// </summary>
        protected bool IsAuthenticated => CurrentUser.IsAuthenticated;

        /// <summary>
        /// 当前用户所有声明
        /// </summary>
        protected IEnumerable<Claim>? UserClaims => CurrentUser.Claims;

        #endregion

        #region 统一响应方法

        /// <summary>
        /// 返回成功响应
        /// </summary>
        protected IActionResult Success(string message = "操作成功")
        {
            return Ok(ApiResult.SuccessResult(message));
        }

        /// <summary>
        /// 返回成功响应（带数据）
        /// </summary>
        protected IActionResult Success<T>(T data, string message = "操作成功")
        {
            return Ok(ApiResult<T>.SuccessResult(data, message));
        }

        /// <summary>
        /// 返回成功响应（带分页数据）
        /// </summary>
        protected IActionResult Success<T>(IEnumerable<T> data, int total, int pageIndex, int pageSize, string message = "查询成功")
        {
            return Ok(PagedResult<T>.SuccessResult(data, total, pageIndex, pageSize, message));
        }

        /// <summary>
        /// 返回失败响应
        /// </summary>
        protected IActionResult Fail(string message, int code = 400)
        {
            return BadRequest(ApiResult.FailResult(message, code));
        }

        /// <summary>
        /// 返回失败响应（带错误详情）
        /// </summary>
        protected IActionResult Fail(string message, object errors, int code = 400)
        {
            var result = ApiResult.FailResult(message, code);
            result.Errors = errors;
            return BadRequest(result);
        }

        /// <summary>
        /// 返回未授权响应
        /// </summary>
        protected IActionResult Unauthorized(string message = "未授权访问")
        {
            return base.Unauthorized(ApiResult.FailResult(message, 401));
        }

        /// <summary>
        /// 返回禁止访问响应
        /// </summary>
        protected IActionResult Forbidden(string message = "禁止访问")
        {
            return StatusCode(403, ApiResult.FailResult(message, 403));
        }

        /// <summary>
        /// 返回未找到响应
        /// </summary>
        protected IActionResult NotFound(string message = "资源未找到")
        {
            return base.NotFound(ApiResult.FailResult(message, 404));
        }

        #endregion

        #region MediatR快捷方法

        /// <summary>
        /// 执行命令并返回统一格式响应
        /// </summary>
        protected async Task<IActionResult> ExecuteCommand<TResponse>(IRequest<TResponse> command)
            where TResponse : class
        {
            try
            {
                var result = await Mediator.Send(command);
                
                // 如果结果实现了IApiResult接口，直接返回
                if (result is IApiResult apiResult)
                {
                    return apiResult.Success ? Ok(result) : BadRequest(result);
                }

                // 否则包装为成功响应
                return Success(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.LogWarning(ex, "未授权访问: {Message}", ex.Message);
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "执行命令时发生错误: {Message}", ex.Message);
                return Fail("操作失败", 500);
            }
        }

        /// <summary>
        /// 执行查询并返回统一格式响应
        /// </summary>
        protected async Task<IActionResult> ExecuteQuery<TResponse>(IRequest<TResponse> query)
            where TResponse : class
        {
            try
            {
                var result = await Mediator.Send(query);

                if (result == null)
                {
                    return NotFound("未找到相关数据");
                }

                // 如果结果实现了IApiResult接口，直接返回
                if (result is IApiResult apiResult)
                {
                    return apiResult.Success ? Ok(result) : BadRequest(result);
                }

                return Success(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "执行查询时发生错误: {Message}", ex.Message);
                return Fail("查询失败", 500);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查用户是否具有指定角色
        /// </summary>
        protected bool HasRole(string role)
        {
            return CurrentUser.IsInRole(role);
        }

        /// <summary>
        /// 检查用户是否具有任一角色
        /// </summary>
        protected bool HasAnyRole(params string[] roles)
        {
            return roles.Any(role => CurrentUser.IsInRole(role));
        }

        /// <summary>
        /// 检查用户是否具有所有角色
        /// </summary>
        protected bool HasAllRoles(params string[] roles)
        {
            return roles.All(role => CurrentUser.IsInRole(role));
        }

        /// <summary>
        /// 获取指定类型的Claim值
        /// </summary>
        protected string? GetClaimValue(string claimType)
        {
            return UserClaims?.FirstOrDefault(c => c.Type == claimType)?.Value;
        }

        /// <summary>
        /// 验证模型状态并返回错误响应
        /// </summary>
        protected IActionResult ValidateModelState()
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                return Fail("验证失败", errors);
            }

            return null!;
        }

        /// <summary>
        /// 记录操作日志
        /// </summary>
        protected void LogOperation(string operation, object? data = null)
        {
            Logger.LogInformation(
                "用户 {UserId} ({UserName}) 执行操作: {Operation}, 数据: {@Data}",
                UserId,
                UserName,
                operation,
                data
            );
        }

        #endregion
        
        protected bool HasPermission(string code) => 
            User.Claims.Any(c => c.Type == "perm" && c.Value == code);
    }
}