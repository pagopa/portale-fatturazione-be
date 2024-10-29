using System.Data;
using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

public static class ReflectionExtensions
{
    public static MemoryStream ToExcel(this DataSet ds)
    {
        var memoryStream = new MemoryStream();
        using (var workbook = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = workbook.AddWorkbookPart();
            workbook.WorkbookPart!.Workbook = new Workbook();
            workbook.WorkbookPart.Workbook.Sheets = new Sheets();

            var maxDigitFont = 11; // weight font

            // add styles
            workbook.AddStyleSheet();

            uint sheetId = 1;

            foreach (DataTable table in ds.Tables)
            {
                var numbersOfChars = new Dictionary<int, int?>();
                for (var j = 0; j < table.Columns.Count; j++)
                {
                    var len = table.Columns[j].Caption.Length;
                    numbersOfChars.TryGetValue(j, out var value);
                    if (value == null)
                        numbersOfChars.TryAdd(j, len);
                    else if (value < len)
                        numbersOfChars[j] = len;
                }

                var lp = 1;

                var sheetPart = workbook.WorkbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                sheetPart.Worksheet = new Worksheet(sheetData);

                var sheets = workbook.WorkbookPart.Workbook.GetFirstChild<Sheets>();
                var relationshipId = workbook.WorkbookPart.GetIdOfPart(sheetPart);

                if (sheets!.Elements<Sheet>().Any())
                    sheetId = sheets.Elements<Sheet>().Select(s => s.SheetId!.Value).Max() + 1;

                var sheet = new Sheet() { Id = relationshipId, SheetId = sheetId, Name = table.TableName };

                sheets.Append(sheet);

                var headerRow = new Row();
                var columns = new Dictionary<string, XCellStyle>();
                var clmns = new Columns();
                var index = 0;
                foreach (DataColumn column in table.Columns)
                {
                    columns.Add(column.ColumnName, (XCellStyle)column.ExtendedProperties["Style"]!);
                    var widthPixels = Math.Truncate((256 * numbersOfChars[index]!.Value + Math.Truncate(128f / maxDigitFont)) / 256f * maxDigitFont);
                    var width = Math.Truncate(((widthPixels - 5f) / maxDigitFont * 100f + 0.5f) / 100f);
                    var cln = new Column
                    {
                        Min = Convert.ToUInt32(index + 1),
                        Max = Convert.ToUInt32(index + 1),
                        Width = width + 5,
                        CustomWidth = true,
                        Style = Convert.ToUInt32(0),
                    };
                    clmns.Append(cln);
                    index++;
                }

                var sheetdata = sheetPart.Worksheet.GetFirstChild<SheetData>();
                sheetPart.Worksheet.InsertBefore(clmns, sheetdata);

                var freezeRow = lp;
                foreach (DataColumn column in table.Columns)
                {
                    var cell = new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(column.Caption),
                        StyleIndex = Convert.ToUInt32(XCellStyle.Header),
                    };
                    headerRow.AppendChild(cell);
                }
                sheetData.AppendChild(headerRow);

                foreach (DataRow dsrow in table.Rows)
                {
                    var newRow = new Row();
                    foreach (var col in columns)
                    {
                        var (cellType, value, type) = TypeFinder.Get(dsrow[col.Key]!);
                        CellValue? cellvalue = null;
                        var cellvalues = cellType;
                        var styleIndex = Convert.ToUInt32(col.Value);
                        if (value == null)
                            cellvalue = new CellValue(string.Empty);
                        else if (type == typeof(decimal))
                        {
                            cellvalues = CellValues.Number;
                            styleIndex = Convert.ToUInt32(XCellStyle.Digit14);
                            cellvalue = new CellValue(Convert.ToDecimal(value));
                        }
                        else if (type == typeof(DateTime))
                        {
                            cellvalues = CellValues.Date;
                            cellvalue = new CellValue(((DateTime)value).ToString("yyyy-MM-dd"));
                            styleIndex =  Convert.ToUInt32(XCellStyle.StandardDateTime);
                        }
                        else
                            cellvalue = new CellValue((dynamic)value);
                        var cell = new Cell
                        {
                            DataType = cellvalues,
                            CellValue = cellvalue,
                            StyleIndex = styleIndex,
                        };
                        newRow.AppendChild(cell);
                    }
                    sheetData.AppendChild(newRow);
                }
            }
        }
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }

    internal static List<HeaderPagoPAAttribute> GetHeaders<T>()
    {
        var list = new List<HeaderPagoPAAttribute>();

        var myPropertyInfo = typeof(T).GetProperties();
        for (var i = 0; i < myPropertyInfo.Length; i++)
        {
            var customAttribute = (HeaderPagoPAAttribute)Attribute.GetCustomAttribute(myPropertyInfo[i], typeof(HeaderPagoPAAttribute))!;
            if (customAttribute != null)
            {
                customAttribute.Type = myPropertyInfo[i].PropertyType;
                customAttribute.Name = myPropertyInfo[i].Name;
                list.Add(customAttribute);
            }
        }
        return [.. list.OrderBy(x => x.Order)];
    }

    internal static (DataTable, List<HeaderPagoPAAttribute>) ToTable<T>(string? tableName = "")
    {
        var headers = GetHeaders<T>();
        var table = new DataTable(string.IsNullOrEmpty(tableName)?nameof(T): tableName);

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


    static IEnumerable<PropertyInfo> GetPropertiesWithAttribute<T, TAttribute>() where TAttribute : Attribute
    {
        return typeof(T).GetProperties()
                        .Where(prop => prop.GetCustomAttribute<TAttribute>() != null);
    }

    public static DataTable FillTable<T>(this IEnumerable<T> data, string tableName)
    { 
        var (table, headers) = ToTable<T>(tableName); 
        DataRow row;
        foreach (var d in data)
        {
            row = table.NewRow();
            foreach (var hh in headers)
                row[hh.Name!] = d!.GetType().GetProperty(hh.Name!)!?.GetValue(d, null) ?? DBNull.Value;
            table.Rows.Add(row);
        } 
        return table;
    }

    public static DataSet FillPagoPAOneSheet<T>(this IEnumerable<T> data, string tableName = "")
    {
        var ds = new DataSet();
        var (table, headers) = ToTable<T>(tableName);
        DataRow row;
        foreach (var d in data)
        {
            row = table.NewRow();
            foreach (var hh in headers)
                row[hh.Name!] = d!.GetType().GetProperty(hh.Name!)!?.GetValue(d, null) ?? DBNull.Value;
            table.Rows.Add(row);
        }
        ds.Tables.Add(table);
        return ds;
    }
}