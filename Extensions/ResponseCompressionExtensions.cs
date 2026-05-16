using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions
{
    /// <summary>
    /// 响应压缩配置扩展
    /// </summary>
    public static class ResponseCompressionExtensions
    {
        /// <summary>
        /// 添加响应压缩支持
        /// </summary>
        public static IServiceCollection AddResponseCompressionSupport(this IServiceCollection services)
        {
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                
                // 需要压缩的MIME类型
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
                {
                    "application/json",
                    "application/xml",
                    "text/plain",
                    "text/html",
                    "text/css",
                    "text/javascript",
                    "application/javascript",
                    "image/svg+xml"
                });
            });

            // 配置Brotli压缩
            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

            // 配置Gzip压缩
            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });

            return services;
        }
    }
}