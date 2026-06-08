using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RiverLi.DDD.Core.Application.Common.Models;  

namespace RiverLi.Blog.Infrastructure.Shared.Exceptions; 

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "全局异常捕获: {Message}", exception.Message);

        var statusCode = StatusCodes.Status500InternalServerError;
        var message = "服务器内部发生未预期的错误，请联系管理员";
        object? errors = null;

        switch (exception)
        {
            case UnauthorizedAccessException _:
                statusCode = StatusCodes.Status403Forbidden;
                message = exception.Message;
                break;
            case InvalidCredentialException _:
                statusCode = StatusCodes.Status401Unauthorized;
                message = exception.Message;
                break;
            case KeyNotFoundException _:
                statusCode = StatusCodes.Status404NotFound;
                message = exception.Message;
                break;
            case InvalidOperationException _:
                statusCode = StatusCodes.Status400BadRequest;
                message = exception.Message;
                break;
            case ValidationException validationEx:
                statusCode = StatusCodes.Status400BadRequest;
                message = "数据验证失败";
                errors = validationEx.ValidationResult.ErrorMessage;
                break;
        }

        var response = ApiResult.FailResult(message, statusCode);
        if (errors != null) response.Errors = errors;

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true; 
    }
}