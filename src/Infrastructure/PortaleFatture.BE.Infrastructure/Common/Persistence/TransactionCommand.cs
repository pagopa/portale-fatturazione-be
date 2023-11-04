using System.Data;

namespace PortaleFatture.BE.Infrastructure.Common.Persistence;
public class TransactionCommand
{
    public bool Create { get; set; } = false;
    public IsolationLevel Level { get; set; } = IsolationLevel.ReadCommitted;
}