using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RiverLi.Blog.Infrastructure.Shared.Auth;
using System.Text;
using RiverLi.DDD.Core.Application.Common.Interfaces;

namespace RiverLi.Blog.Infrastructure.Shared.Extensions
{
    public static class AuthenticationServiceCollectionExtensions
    {
        /// <summary>
        /// 为微服务注册统一的 JWT 认证机制
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddRiverJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. 读取配置
            var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

            // 如果配置不存在，抛出异常或记录警告（这里选择抛出，因为没有 Auth 服务无法运行）
            if (jwtOptions == null || string.IsNullOrEmpty(jwtOptions.SecretKey))
            {
                throw new ArgumentNullException(nameof(jwtOptions), "JWT 配置缺失，请检查 appsettings.json 中的 Jwt 节点");
            }

            var key = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

            // 2. 注册认证服务
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // 开发环境允许 http，生产环境建议 true
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // 移除默认的 5 分钟时间偏差，让过期时间更精确
                };

                // 可选：添加事件处理，例如 Token 验证失败时的自定义响应：
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        Console.WriteLine($"📨 1. 收到消息: {context.Token?.Substring(0, 5)}...");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"❌ 2. 验证失败: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("✅ 2. 验证通过");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        // 如果执行到这里，说明没有通过认证，查看响应头里的 error 信息
                        Console.WriteLine($"⚠️ 3. 触发挑战 (401): {context.Error}, {context.ErrorDescription}");
                        return Task.CompletedTask;
                    }
                };
                /*
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
                */
            });
            // 注册 HttpContextAccessor (CurrentUser 依赖它)
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUser>();
            return services;
        }
    }
}