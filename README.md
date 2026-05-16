# RiverLi.Blog.Infrastructure.Shared

RiverLi.Blog 微服务集群通用 DDD 基础设施共享库，包含 EFCore 实现、MassTransit 实现和 Redis 实现。

## 概述

这是一个为 RiverLi.Blog 微服务集群提供通用基础设施组件的共享库。它包含了常见的基础设施功能实现，如数据访问、缓存、事件总线等。

## 功能特性

- **数据访问**: 基于 Entity Framework Core 的仓储模式实现
- **缓存**: 基于 Redis 的缓存服务
- **事件总线**: 基于 MassTransit 和 RabbitMQ 的事件发布机制
- **认证**: JWT 认证相关配置
- **健康检查**: 集成 ASP.NET Core 健康检查功能
- **日志记录**: 使用 Serilog 进行结构化日志记录
- **弹性策略**: 集成 Polly 提供重试和断路器功能
- **分布式追踪**: OpenTelemetry 支持

## 依赖项

- .NET 9.0
- Entity Framework Core
- MassTransit (RabbitMQ)
- StackExchange Redis
- MediatR
- Serilog
- OpenTelemetry
- Health Checks

## 安装

```bash
Install-Package RiverLi.Blog.Infrastructure.Shared
```
