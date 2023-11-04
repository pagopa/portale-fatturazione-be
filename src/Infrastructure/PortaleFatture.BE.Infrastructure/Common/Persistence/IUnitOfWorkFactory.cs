using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence;

public interface IUnitOfWorkFactory
{
    Task<IUnitOfWork> Create(bool transactional = false, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
} 