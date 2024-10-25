using DocumentFormat.OpenXml.Spreadsheet;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

public class TypeFinder
{
    private static readonly Dictionary<Type, CellValues> _matches = new()
    {
        { typeof(long?) ,  CellValues.Number},
        { typeof(double?) ,  CellValues.Number},
        { typeof(int?) ,  CellValues.Number},
        { typeof(short?) ,  CellValues.Number},
        { typeof(decimal?) ,  CellValues.Number},
        { typeof(byte?) ,  CellValues.Number},
        { typeof(bool?) ,  CellValues.Boolean},
        { typeof(DateTime?) ,  CellValues.Date},
        { typeof(DateTimeOffset?) ,  CellValues.Date},
        { typeof(string) ,  CellValues.String},
        { typeof(long) ,  CellValues.Number},
        { typeof(double) ,  CellValues.Number},
        { typeof(int) ,  CellValues.Number},
        { typeof(short) ,  CellValues.Number},
        { typeof(decimal) ,  CellValues.Number},
        { typeof(byte) ,  CellValues.Number},
        { typeof(bool) ,  CellValues.Boolean},
        { typeof(DateTime) ,  CellValues.Date},
        { typeof(DateTimeOffset) ,  CellValues.Date}
    };

    public static (CellValues, T, Type) Get<T>(T value)
    {
        var type = Nullable.GetUnderlyingType(value!.GetType()!) ?? value!.GetType();
        var check = _matches.TryGetValue(type, out var cellValues);
        if (check)
            return (cellValues, value, type);
        return (CellValues.String, default(T), typeof(string))!;
    }
}