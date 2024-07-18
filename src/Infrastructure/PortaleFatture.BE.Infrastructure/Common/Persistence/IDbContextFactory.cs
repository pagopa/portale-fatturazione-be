using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence;

public interface IDbContextFactory
{
    Task<IDbContext> Create(bool transactional = false, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);
} 