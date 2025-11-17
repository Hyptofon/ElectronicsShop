using System.Data;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

public class DbTransactionWrapper : IDbTransactionWrapper
{
    private readonly IDbContextTransaction _transaction;

    public DbTransactionWrapper(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public void Commit() => _transaction.Commit();
    
    public async Task CommitAsync(CancellationToken cancellationToken = default) 
        => await _transaction.CommitAsync(cancellationToken);

    public void Rollback() => _transaction.Rollback();
    
    public async Task RollbackAsync(CancellationToken cancellationToken = default) 
        => await _transaction.RollbackAsync(cancellationToken);

    public void Dispose() => _transaction.Dispose();

    public IDbConnection? Connection => _transaction.GetDbTransaction().Connection;
    public IsolationLevel IsolationLevel => _transaction.GetDbTransaction().IsolationLevel;
}