using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;
using System.Diagnostics;

namespace RiverLi.Blog.Infrastructure.Shared.Logging
{
    /// <summary>
    /// 详细的日志中间件 (记录所有 HTTP 请求)
    /// </summary>
    public class RiverRequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RiverRequestLoggingMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            // 为每个请求自动生成 TraceId (全链路追踪的关键)
            var traceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

            // 使用 LogContext 压入属性，该请求后续所有的 Log.LogInfo 都会带上这个 TraceId
            using (LogContext.PushProperty("TraceId", traceId))
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    await _next(context);
                    sw.Stop();

                    // 只记录有意义的请求（忽略静态资源等）
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        Log.Information("HTTP {Method} {Path} responded {StatusCode} in {Elapsed:0.0000} ms",
                            context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.Elapsed.TotalMilliseconds);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "HTTP {Method} {Path} failed after {Elapsed:0.0000} ms",
                        context.Request.Method, context.Request.Path, sw.Elapsed.TotalMilliseconds);
                    throw;
                }
            }
        }
        public async Task Invoke2(HttpContext context)
        {
            // 获取 OpenTelemetry 自动生成的当前 TraceId
            // 如果没有 OTel，则回退到手动生成的 GUID
            var traceId = Activity.Current?.TraceId.ToString()
                          ?? context.Request.Headers["X-Trace-Id"].FirstOrDefault()
                          ?? Guid.NewGuid().ToString();

            // 关键：将 TraceId 推送到 Serilog 的日志上下文
            using (LogContext.PushProperty("TraceId", traceId))
            {
                // 同时也把 TraceId 返回给前端，方便用户反馈问题
                context.Response.Headers["X-Trace-Id"] = traceId;

                await _next(context);
            }
        }
    }
}