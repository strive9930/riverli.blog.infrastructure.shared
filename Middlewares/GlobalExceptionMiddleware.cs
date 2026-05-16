using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using RiverLi.DDD.Core.Application.Common.Models;

namespace RiverLi.Blog.Infrastructure.Shared.Middlewares
{
    /// <summary>
    /// 全局异常处理中间件
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "未处理的异常: {Message}", exception.Message);

            var response = context.Response;
            response.ContentType = "application/json";

            ApiResult result;

            switch (exception)
            {
                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    result = ApiResult.FailResult(exception.Message, 401);
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    result = ApiResult.FailResult(exception.Message, 404);
                    break;

                case ArgumentException:
                //case ArgumentNullException:
                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    result = ApiResult.FailResult(exception.Message, 400);
                    break;

                case FluentValidation.ValidationException validationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    var errors = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        );
                    result = ApiResult.FailResult("验证失败", 400);
                    result.Errors = errors;
                    break;

                case ApplicationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    result = ApiResult.FailResult(exception.Message, 400);
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    // 生产环境不要暴露详细错误信息
                    var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
                    result = ApiResult.FailResult(
                        isDevelopment ? exception.Message : "服务器内部错误",
                        500
                    );
                    if (isDevelopment)
                    {
                        result.Errors = new
                        {
                            exception.StackTrace,
                            InnerException = exception.InnerException?.Message
                        };
                    }
                    break;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            await response.WriteAsync(JsonSerializer.Serialize(result, options));
        }
    }
}