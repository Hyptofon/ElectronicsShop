using System.Data;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}

// WRAPPER ДЛЯ ТРАНЗАКЦІЙ
public interface IDbTransactionWrapper : IDbTransaction
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}