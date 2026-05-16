using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//添加一个中间件，记录每个 API 的耗时和入参，方便排查性能问题。
namespace RiverLi.Blog.Infrastructure.Shared.Extensions
{
    public class RequestLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLogMiddleware> _logger;

        public RequestLogMiddleware(RequestDelegate next, ILogger<RequestLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await _next(context);
                sw.Stop();
                // 记录 200 以上的慢请求或异常
                if (sw.ElapsedMilliseconds > 500)
                    _logger.LogWarning("慢接口警告: {Path} 耗时 {Ms}ms", context.Request.Path, sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "接口异常: {Path}", context.Request.Path);
                throw;
            }
        }
    }
}
