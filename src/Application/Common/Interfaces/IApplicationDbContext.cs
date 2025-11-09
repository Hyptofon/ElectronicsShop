using System.Data;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}