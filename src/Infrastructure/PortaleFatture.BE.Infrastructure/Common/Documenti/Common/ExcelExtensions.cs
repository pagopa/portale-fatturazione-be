using System.Data;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PortaleFatture.BE.Infrastructure.Common.Documenti.Common;

public static class ExcelExtensions
{
    #region simple formats

    internal static string To(this bool? value)
    {
        if (value.HasValue && value.Value == true)
            return "SI";
        else if (value.HasValue && value.Value == false)
            return "NO";
        return string.Empty;
    }

    #endregion

    internal static ForegroundColor TranslateForeground(System.Drawing.Color fillColor)
    {
        return new ForegroundColor()
        {
            Rgb = new HexBinaryValue()
            {
                Value = String.Format("{0:X2}{1:X2}{2:X2}{3:X3}", fillColor.R, fillColor.G, fillColor.B, fillColor.A)
            }
        };
    } 
    internal static WorkbookStylesPart AddStyleSheet(this SpreadsheetDocument spreadsheet)
    {
        var stylesheet = spreadsheet.WorkbookPart!.AddNewPart<WorkbookStylesPart>();
        var workbookstylesheet = new Stylesheet();

        #region Number format
        uint DATETIME_FORMAT = 164;
        uint DIGITS4_FORMAT = 165;
        var numberingFormats = new NumberingFormats();
        numberingFormats.Append(new NumberingFormat // Datetime format
        {
            NumberFormatId = UInt32Value.FromUInt32(DATETIME_FORMAT),
            FormatCode = StringValue.FromString("dd/mm/yyyy hh:mm:ss")
        });
        numberingFormats.Append(new NumberingFormat // four digits format
        {
            NumberFormatId = UInt32Value.FromUInt32(DIGITS4_FORMAT),
            FormatCode = StringValue.FromString("0000")
        });
        numberingFormats.Count = UInt32Value.FromUInt32((uint)numberingFormats.ChildElements.Count);
        #endregion

        #region Fonts
        var fonts = new Fonts();
        fonts.Append(new DocumentFormat.OpenXml.Spreadsheet.Font()  // Font index 0 - default
        {
            FontName = new FontName { Val = StringValue.FromString("Calibri") },
            FontSize = new FontSize { Val = DoubleValue.FromDouble(11) }
        });
        fonts.Append(new DocumentFormat.OpenXml.Spreadsheet.Font()  // Font index 1
        {
            FontName = new FontName { Val = StringValue.FromString("Arial") },
            FontSize = new FontSize { Val = DoubleValue.FromDouble(11) },
            Bold = new Bold()
        });
        fonts.Count = UInt32Value.FromUInt32((uint)fonts.ChildElements.Count);
        #endregion

        #region Fills
        var fills = new Fills();
        fills.Append(new Fill() // Fill index 0
        {
            PatternFill = new PatternFill { PatternType = PatternValues.None }
        });
        fills.Append(new Fill() // Fill index 1
        {
            PatternFill = new PatternFill { PatternType = PatternValues.Gray125 }
        });
        fills.Append(new Fill() // Fill index 2
        {
            PatternFill = new PatternFill
            {
                PatternType = PatternValues.Solid,
                ForegroundColor = TranslateForeground(System.Drawing.Color.LightBlue),
                BackgroundColor = new BackgroundColor { Rgb = TranslateForeground(System.Drawing.Color.LightBlue).Rgb }
            }
        });
        fills.Append(new Fill() // Fill index 3
        {
            PatternFill = new PatternFill
            {
                PatternType = PatternValues.Solid,
                ForegroundColor = TranslateForeground(System.Drawing.Color.LightSkyBlue),
                BackgroundColor = new BackgroundColor { Rgb = TranslateForeground(System.Drawing.Color.LightBlue).Rgb }
            }
        });
        fills.Count = UInt32Value.FromUInt32((uint)fills.ChildElements.Count);
        #endregion

        #region Borders
        var borders = new Borders();
        borders.Append(new Border   // Border index 0: no border
        {
            LeftBorder = new LeftBorder(),
            RightBorder = new RightBorder(),
            TopBorder = new TopBorder(),
            BottomBorder = new BottomBorder(),
            DiagonalBorder = new DiagonalBorder()
        });
        borders.Append(new Border    //Boarder Index 1: All
        {
            LeftBorder = new LeftBorder { Style = BorderStyleValues.Thin },
            RightBorder = new RightBorder { Style = BorderStyleValues.Thin },
            TopBorder = new TopBorder { Style = BorderStyleValues.Thin },
            BottomBorder = new BottomBorder { Style = BorderStyleValues.Thin },
            DiagonalBorder = new DiagonalBorder()
        });
        borders.Append(new Border   // Boarder Index 2: Top and Bottom
        {
            LeftBorder = new LeftBorder(),
            RightBorder = new RightBorder(),
            TopBorder = new TopBorder { Style = BorderStyleValues.Thin },
            BottomBorder = new BottomBorder { Style = BorderStyleValues.Thin },
            DiagonalBorder = new DiagonalBorder()
        });
        borders.Count = UInt32Value.FromUInt32((uint)borders.ChildElements.Count);
        #endregion

        #region Cell Style Format
        var cellStyleFormats = new CellStyleFormats();
        cellStyleFormats.Append(new CellFormat  // Cell style format index 0: no format
        {
            NumberFormatId = 0,
            FontId = 0,
            FillId = 0,
            BorderId = 0,
            FormatId = 0
        });
        cellStyleFormats.Count = UInt32Value.FromUInt32((uint)cellStyleFormats.ChildElements.Count);
        #endregion

        #region Cell format
        var cellFormats = new CellFormats();
        cellFormats.Append(new CellFormat()   // Cell format index 0
        {
            FontId = 0,
            FillId = 0,
            BorderId = 0,
            NumberFormatId = 0,
            FormatId = 0
        });
        cellFormats.Append(new CellFormat()
        {
            Alignment = new Alignment() { WrapText = true }
        });
        cellFormats.Append(new CellFormat   // CellFormat index 2
        {
            NumberFormatId = 14,        // 14 = 'mm-dd-yy'. Standard Date format;
            FontId = 0,
            FillId = 0,
            BorderId = 0,
            FormatId = 0,
            ApplyNumberFormat = BooleanValue.FromBoolean(true)
        });
        cellFormats.Append(new CellFormat   // Cell format index 3: Standard Number format with 2 decimal placing
        {
            NumberFormatId = 4,        // 4 = '#,##0.00';
            FontId = 0,
            FillId = 0,
            BorderId = 0,
            FormatId = 0,
            ApplyNumberFormat = BooleanValue.FromBoolean(true)
        });
        cellFormats.Append(new CellFormat   // Cell formt index 4
        {
            NumberFormatId = DATETIME_FORMAT,        // 164 = 'dd/mm/yyyy hh:mm:ss'. Standard Datetime format;
            FontId = 0,
            FillId = 0,
            BorderId = 0,
            FormatId = 0,
            ApplyNumberFormat = BooleanValue.FromBoolean(true)
        });
        cellFormats.Append(new CellFormat   // Cell format index 5
        {
            NumberFormatId = 3, // 3   #,##0
            FontId = 0,
            FillId = 0,
            BorderId = 0,
            FormatId = 0,
            ApplyNumberFormat = BooleanValue.FromBoolean(true)
        });
        cellFormats.Append(new CellFormat   // Cell format index 6
        {
            NumberFormatId = 10,    // 10  0.00 %,
            FontId = 0,
            FillId = 0,
            BorderId = 0,
            FormatId = 0,
            ApplyNumberFormat = BooleanValue.FromBoolean(true)
        });
        cellFormats.Append(new CellFormat   // Cell format index 7
        {
            NumberFormatId = DIGITS4_FORMAT,    // Format cellas 4 digits. If less than 4 digits, prepend 0 in front
            FontId = 0,
            FillId = 0,
            BorderId = 0,
            FormatId = 0,
            ApplyNumberFormat = BooleanValue.FromBoolean(true)
        });
        cellFormats.Append(new CellFormat  // Cell format header
        {
            NumberFormatId = 49,
            FontId = 1,
            FillId = 0,
            BorderId = 2,
            FormatId = 0,
            ApplyNumberFormat = BooleanValue.FromBoolean(true),
            Alignment = new Alignment() { WrapText = false, Horizontal = HorizontalAlignmentValues.Center }
        });
        cellFormats.Count = UInt32Value.FromUInt32((uint)cellFormats.ChildElements.Count);
        #endregion


        // Append FONTS, FILLS , BORDERS & CellFormats to stylesheet <Preserve the ORDER>
        workbookstylesheet.Append(fonts);
        workbookstylesheet.Append(fills);
        workbookstylesheet.Append(borders);
        workbookstylesheet.Append(cellFormats);

        // Finalize
        stylesheet.Stylesheet = workbookstylesheet;
        stylesheet.Stylesheet.Save();

        return stylesheet;
    }

    public static bool SheetExist(this SpreadsheetDocument doc, string sheetName)
    {
        if (doc == null) throw new ArgumentNullException("SpreadsheetDocument");
        if (doc.WorkbookPart == null) throw new ArgumentNullException("WorkbookPart");
        var wbPart = doc.WorkbookPart;
        Sheet sheet = wbPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name == sheetName)!;
        return sheet != null;
    }

    public static MemoryStream ToExcel(this DataSet ds)
    {
        var memoryStream = new MemoryStream();
        using (var workbook = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = workbook.AddWorkbookPart();
            workbook.WorkbookPart!.Workbook = new Workbook();
            workbook.WorkbookPart.Workbook.Sheets = new Sheets();

            int maxDigitFont = 11; // weight font

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

                int lp = 1;

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
                int index = 0;
                foreach (DataColumn column in table.Columns)
                {
                    columns.Add(column.ColumnName, (XCellStyle)column.ExtendedProperties["Style"]!);
                    var widthPixels = Math.Truncate(((256 * numbersOfChars[index]!.Value + Math.Truncate(128f / maxDigitFont)) / 256f) * maxDigitFont);
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
                        var cell = new Cell
                        {
                            DataType = cellType,
                            CellValue = new CellValue((dynamic)value),
                            StyleIndex = Convert.ToUInt32(col.Value),
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
} 