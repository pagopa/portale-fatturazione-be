using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries.Persistence.Builder
{
    public class SortParamSQLBuilder
    {
        public SortParamSQLBuilder(string? columnName, string? order) {
            ColumnName = columnName;
            Order = order;
        }
        public string? ColumnName { get;}
        public string? Order { get; }
    }
}
