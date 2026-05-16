using System.Data;
using RiverLi.DDD.Core.Domain.Repositories;

namespace RiverLi.Blog.Infrastructure.Shared.Data;

/// <summary>
    /// Dapper工作单元实现
    /// 注意: Dapper是轻量级ORM，没有变更追踪，需要手动管理事务
    /// </summary>
    public class DapperUnitOfWork : IUnitOfWork
    {
        private readonly IDbConnection _connection;
        private IDbTransaction? _transaction;
        private bool _disposed;

        public DapperUnitOfWork(IDbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        public IDbTransaction BeginTransaction()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("事务已经开始");
            }

            if (_connection.State != ConnectionState.Open)
            {
                _connection.Open();
            }

            _transaction = _connection.BeginTransaction();
            return _transaction;
        }

        /// <summary>
        /// 获取当前事务
        /// </summary>
        public IDbTransaction? GetTransaction()
        {
            return _transaction;
        }

        /// <summary>
        /// 保存更改(提交事务)
        /// </summary>
        public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null)
            {
                // 如果没有事务，直接返回true (Auto-commit模式)
                return await Task.FromResult(true);
            }

            try
            {
                _transaction.Commit();
                return await Task.FromResult(true);
            }
            catch
            {
                _transaction.Rollback();
                throw;
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _transaction?.Dispose();
                    _connection?.Dispose();
                }

                _disposed = true;
            }
        }
    }