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
        table.ExtendedProperties[ExcelExtensions.SubtotalRowProperty] = true; // PF-753: totali scritti con SUBTOTAL
        return table;
    }

    /// <param name="dataTableName">Nome del foglio. Senza, il foglio si chiama <c>"T"</c> (<c>nameof(T)</c> in
    /// <see cref="ToTablev2{T}"/>): PF-1029 lo passa per i report REL.</param>
    public static DataSet FillOneSheetWithTotalsRel<T>(this IEnumerable<T> data, int startToSum = 6, string? dataTableName = null)
    {
        var ds = new DataSet();
        var (table, headers) = ToTablev2<T>(dataTableName);
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
        table.ExtendedProperties[ExcelExtensions.SubtotalRowProperty] = true; // PF-753: totali scritti con SUBTOTAL
        ds.Tables.Add(table);
        return ds;
    }

    public static DataSet FillOneSheetv2<T>(this IEnumerable<T> data, string? dataTableName = null)
    {
        var ds = new DataSet();

        var (table, headers) = ToTablev2<T>(dataTableName);
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

    /// <summary>
    /// PF-1029: aggiunge in coda a una tabella già riempita (<see cref="FillOneTable{T}"/>, <see cref="FillOneSheetv2{T}"/>)
    /// la riga vuota e la riga "Totali:", come <see cref="FillTableWithTotalsRel{T}"/>, ma sommando <b>solo</b> le colonne
    /// indicate. Serve dove fra le colonne numeriche ce ne sono che non vanno sommate (anni, mesi, numeri fattura, saldi
    /// progressivi come il credito cumulato). La tabella è marcata per la scrittura con SUBTOTAL.
    /// </summary>
    /// <param name="table">Tabella con la prima colonna di testo, dove va l'etichetta "Totali:".</param>
    /// <param name="colonne">Nomi delle proprietà (= colonne) da sommare, di tipo decimal o int.</param>
    /// <exception cref="ArgumentException">Colonna assente o non numerica, o prima colonna non di testo: è un errore di
    /// programmazione, meglio un'eccezione che un totale che sparisce in silenzio.</exception>
    public static DataTable AddTotali(this DataTable table, params string[] colonne)
    {
        if (table.Columns.Count == 0 || table.Columns[0].DataType != typeof(string))
            throw new ArgumentException($"La prima colonna di '{table.TableName}' deve essere di testo per l'etichetta \"Totali:\".", nameof(table));

        var colonneDaSommare = colonne.Select(nome => table.Columns[nome]
            ?? throw new ArgumentException($"Colonna '{nome}' assente in '{table.TableName}'.", nameof(colonne))).ToList();
        foreach (var col in colonneDaSommare.Where(c => c.DataType != typeof(decimal) && c.DataType != typeof(int)))
            throw new ArgumentException($"Colonna '{col.ColumnName}' di '{table.TableName}' non numerica: non si può sommare.", nameof(colonne));

        var righeDati = table.AsEnumerable().ToList();
        table.Rows.Add(table.NewRow());
        var rowTot = table.NewRow();
        rowTot[0] = "Totali:";
        foreach (var col in colonneDaSommare)
            rowTot[col] = col.DataType == typeof(decimal)
                ? righeDati.Sum(r => r.Field<decimal?>(col) ?? 0m)
                : righeDati.Sum(r => r.Field<int?>(col) ?? 0);
        table.Rows.Add(rowTot);
        table.ExtendedProperties[ExcelExtensions.SubtotalRowProperty] = true;
        return table;
    }

    public static DataTable FillOneTable<T>(this IEnumerable<T> data, string? dataTableName = null)
    {
        var (table, headers) = ToTablev2<T>(dataTableName);
        DataRow row;
        foreach (var d in data)
        {
            row = table.NewRow();
            foreach (var hh in headers)
                row[hh.Name!] = d!.GetType().GetProperty(hh.Name!)!.GetValue(d, null) ?? DBNull.Value;

            table.Rows.Add(row);
        }
        return table;
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