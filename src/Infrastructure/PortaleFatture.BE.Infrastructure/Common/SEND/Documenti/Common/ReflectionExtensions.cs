using System.Data;
using System.Reflection;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

public static class ReflectionExtensions
{
    internal static List<HeaderAttributeRECCON> GetHeaderRECCON<T>()
    {
        var list = new List<HeaderAttributeRECCON>();

        var myPropertyInfo = typeof(T).GetProperties();
        for (var i = 0; i < myPropertyInfo.Length; i++)
        {
            var customAttribute = (HeaderAttributeRECCON)Attribute.GetCustomAttribute(myPropertyInfo[i], typeof(HeaderAttributeRECCON))!;
            if (customAttribute != null)
            {
                customAttribute.Type = myPropertyInfo[i].PropertyType;
                customAttribute.Name = myPropertyInfo[i].Name;
                list.Add(customAttribute);
            }
        }
        return [.. list.OrderBy(x => x.Order)];
    }


    internal static List<HeaderAttributev2> GetHeadersv2<T>()
    {
        var list = new List<HeaderAttributev2>();

        var myPropertyInfo = typeof(T).GetProperties();
        for (var i = 0; i < myPropertyInfo.Length; i++)
        {
            var customAttribute = (HeaderAttributev2)Attribute.GetCustomAttribute(myPropertyInfo[i], typeof(HeaderAttributev2))!;
            if (customAttribute != null)
            {
                customAttribute.Type = myPropertyInfo[i].PropertyType;
                customAttribute.Name = myPropertyInfo[i].Name;
                list.Add(customAttribute);
            }
        }
        return [.. list.OrderBy(x => x.Order)];
    }

    internal static List<HeaderAttribute> GetHeaders<T>()
    {
        var list = new List<HeaderAttribute>();

        var myPropertyInfo = typeof(T).GetProperties();
        for (var i = 0; i < myPropertyInfo.Length; i++)
        {
            var customAttribute = (HeaderAttribute)Attribute.GetCustomAttribute(myPropertyInfo[i], typeof(HeaderAttribute))!;
            if (customAttribute != null)
            {
                customAttribute.Type = myPropertyInfo[i].PropertyType;
                customAttribute.Name = myPropertyInfo[i].Name;
                list.Add(customAttribute);
            }
        }
        return [.. list.OrderBy(x => x.Order)];
    }

    internal static (DataTable, List<HeaderAttribute>) ToTable<T>()
    {
        var headers = GetHeaders<T>();
        var table = new DataTable(typeof(T).Name);

        foreach (var hh in headers)
        {
            var column = new DataColumn
            {
                DataType = Nullable.GetUnderlyingType(hh.Type!) ?? hh.Type,
                ColumnName = hh.Name,
                Caption = hh.Caption,
                ReadOnly = hh.ReadOnly
            };
            column.ExtendedProperties.Add("Style", hh.Style);
            table.Columns.Add(column);
        }

        return (table, headers);
    }
    internal static (DataTable, List<HeaderAttribute>) ToTableWithName<T>(string? tableName = "")
    {
        var headers = GetHeaders<T>();
        var table = new DataTable(tableName ?? nameof(T));

        foreach (var hh in headers)
        {
            var column = new DataColumn
            {
                DataType = Nullable.GetUnderlyingType(hh.Type!) ?? hh.Type,
                ColumnName = hh.Name,
                Caption = hh.Caption,
                ReadOnly = hh.ReadOnly
            };
            column.ExtendedProperties.Add("Style", hh.Style);
            table.Columns.Add(column);
        }

        return (table, headers);
    }

    public static dynamic CalculateHeaderAttribute<T>(this T model)
    {
        var selectedProperties = GetPropertiesWithAttribute<T, HeaderAttribute>();

        dynamic dynamicObject = new System.Dynamic.ExpandoObject();
        foreach (var property in selectedProperties)
        {
            ((IDictionary<string, object>)dynamicObject)[property.Name] = property.Name;
        }
        return dynamicObject;
    }

    public static IEnumerable<dynamic> ToHeaderAttributeCsv<T>(this IEnumerable<T> lista)
    {
        var selectedProperties = GetPropertiesWithAttribute<T, HeaderAttribute>();
        return lista.Select(item =>
        {
            dynamic dynamicObject = new System.Dynamic.ExpandoObject();
            foreach (var property in selectedProperties)
            {
                ((IDictionary<string, object>)dynamicObject)[property.Name] = property.GetValue(item)!;
            }
            return dynamicObject;
        });
    }

    public static IEnumerable<dynamic> ToHeaderAttributev2Csv<T>(this IEnumerable<T> lista)
    {
        var selectedProperties = GetPropertiesWithAttribute<T, HeaderAttributev2>();
        return lista.Select(item =>
        {
            dynamic dynamicObject = new System.Dynamic.ExpandoObject();
            foreach (var property in selectedProperties)
            {
                ((IDictionary<string, object>)dynamicObject)[property.Name] = property.GetValue(item)!;
            }
            return dynamicObject;
        });
    }
    static IEnumerable<PropertyInfo> GetPropertiesWithAttribute<T, TAttribute>() where TAttribute : Attribute
    {
        return typeof(T).GetProperties()
                        .Where(prop => prop.GetCustomAttribute<TAttribute>() != null);
    }

    internal static (DataTable, List<HeaderAttributev2>) ToTablev2<T>(string? dataTableName = null)
    {
        var headers = GetHeadersv2<T>();
        var table = new DataTable(dataTableName ?? nameof(T));
        foreach (var hh in headers)
        {
            var column = new DataColumn
            {
                DataType = Nullable.GetUnderlyingType(hh.Type!) ?? hh.Type,
                ColumnName = hh.Name,
                Caption = hh.Caption,
                ReadOnly = hh.ReadOnly
            };
            column.ExtendedProperties.Add("Style", hh.Style);
            table.Columns.Add(column);
        }
        return (table, headers);
    }

    internal static (DataTable, List<HeaderAttributeRECCON>) ToTableRECCON<T>()
    {
        var headers = GetHeaderRECCON<T>();
        var table = new DataTable(nameof(T));
        foreach (var hh in headers)
        {
            var column = new DataColumn
            {
                DataType = Nullable.GetUnderlyingType(hh.Type!) ?? hh.Type,
                ColumnName = hh.Name,
                Caption = hh.Caption,
                ReadOnly = hh.ReadOnly
            };
            column.ExtendedProperties.Add("Style", hh.Style);
            table.Columns.Add(column);
        }
        return (table, headers);
    }

    public static DataSet FillOneSheetWithName<T>(this IEnumerable<T> data, string? tableName = "")
    {
        var ds = new DataSet();
        var (table, headers) = ToTableWithName<T>(tableName);
        DataRow row;
        foreach (var d in data)
        {
            row = table.NewRow();
            foreach (var hh in headers)
            {
                var value = d!.GetType().GetProperty(hh.Name!)!.GetValue(d, null);
                row[hh.Name!] = (value == null || (value is string s && string.IsNullOrEmpty(s)))
                 ? DBNull.Value
                 : value;
            }

            table.Rows.Add(row);
        }
        ds.Tables.Add(table);
        return ds;
    }

    public static DataSet FillOneSheet<T>(this IEnumerable<T> data)
    {
        var ds = new DataSet();
        var (table, headers) = ToTable<T>();
        DataRow row;
        foreach (var d in data)
        {
            row = table.NewRow();
            foreach (var hh in headers)
            {
                var value = d!.GetType().GetProperty(hh.Name!)!.GetValue(d, null);
                row[hh.Name!] = (value == null || (value is string s && string.IsNullOrEmpty(s)))
                 ? DBNull.Value
                 : value;
            } 

            table.Rows.Add(row);
        }
        ds.Tables.Add(table);
        return ds;
    }

    public static DataTable FillTableWithTotalsRel<T>(this IEnumerable<T> data, int startToSum = 6, string? dataTableName = null)
    {
        var (table, headers) = ToTablev2<T>(dataTableName);
        DataRow row;
        foreach (var d in data)
        {
            row = table.NewRow();
            foreach (var hh in headers)
                row[hh.Name!] = d!.GetType().GetProperty(hh.Name!)!?.GetValue(d, null) ?? DBNull.Value;
            table.Rows.Add(row);
        }

        table.Rows.Add(table.NewRow());
        var rowTot = table.NewRow();
        for (var i = 0; i < table.Columns.Count; i++)
        {
            if (i == 0)
            {
                rowTot[i] = "Totali:";
            }
            else
            {
                if (i >= startToSum)
                {
                    if (table.Columns[i].DataType == typeof(decimal))
                        rowTot[i] = table.AsEnumerable().Sum(x => x.Field<decimal?>(table.Columns[i].ColumnName));
                    else if (table.Columns[i].DataType == typeof(int))
                        rowTot[i] = table.AsEnumerable().Sum(x => x.Field<int?>(table.Columns[i].ColumnName));
                    else
                        rowTot[i] = DBNull.Value;
                }
                else
                    rowTot[i] = DBNull.Value;
            }
        }
        table.Rows.Add(rowTot);
        return table;
    }

    public static DataSet FillOneSheetWithTotalsRel<T>(this IEnumerable<T> data, int startToSum = 6)
    {
        var ds = new DataSet();
        var (table, headers) = ToTablev2<T>();
        DataRow row;
        foreach (var d in data)
        {
            row = table.NewRow();
            foreach (var hh in headers)
                row[hh.Name!] = d!.GetType().GetProperty(hh.Name!)!.GetValue(d, null);

            table.Rows.Add(row);
        }

        table.Rows.Add(table.NewRow());
        var rowTot = table.NewRow();
        for (var i = 0; i < table.Columns.Count; i++)
        {
            if (i == 0)
            {
                rowTot[i] = "Totali:";
            }
            else
            {
                if (i >= startToSum)
                {
                    if (table.Columns[i].DataType == typeof(decimal))
                        rowTot[i] = table.AsEnumerable().Sum(x => x.Field<decimal?>(table.Columns[i].ColumnName));
                    else if (table.Columns[i].DataType == typeof(int))
                        rowTot[i] = table.AsEnumerable().Sum(x => x.Field<int?>(table.Columns[i].ColumnName));
                    else
                        rowTot[i] = DBNull.Value;
                }
                else
                    rowTot[i] = DBNull.Value;
            }
        }
        table.Rows.Add(rowTot);
        ds.Tables.Add(table);
        return ds;
    }

    public static DataSet FillOneSheetv2<T>(this IEnumerable<T> data)
    {
        var ds = new DataSet();
        var (table, headers) = ToTablev2<T>();
        DataRow row;
        foreach (var d in data)
        {
            row = table.NewRow();
            foreach (var hh in headers)
                row[hh.Name!] = d!.GetType().GetProperty(hh.Name!)!.GetValue(d, null) ?? DBNull.Value;

            table.Rows.Add(row);
        }
        ds.Tables.Add(table);
        return ds;
    }

    public static DataSet FillOneSheetRECCON<T>(this IEnumerable<T> data)
    {
        var ds = new DataSet();
        var (table, headers) = ToTableRECCON<T>();
        DataRow row;
        foreach (var d in data)
        {
            row = table.NewRow();
            foreach (var hh in headers)
                row[hh.Name!] = d!.GetType().GetProperty(hh.Name!)!.GetValue(d, null);

            table.Rows.Add(row);
        }
        ds.Tables.Add(table);
        return ds;
    }

    public static DataSet FillWorkbook<T>(this IEnumerable<DataTable> dataTables)
    {
        var ds = new DataSet();
        foreach (var d in dataTables)
            ds.Tables.Add(d);
        return ds;
    }
}