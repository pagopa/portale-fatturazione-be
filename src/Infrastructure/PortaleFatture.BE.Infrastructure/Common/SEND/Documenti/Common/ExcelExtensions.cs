using System.Data;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

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
                Value = string.Format("{0:X2}{1:X2}{2:X2}{3:X3}", fillColor.R, fillColor.G, fillColor.B, fillColor.A)
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
        uint DIGITS14_FORMAT = 166;
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
        numberingFormats.Append(new NumberingFormat // 14 digits format
        {
            NumberFormatId = UInt32Value.FromUInt32(DIGITS14_FORMAT),
            FormatCode = StringValue.FromString("0.00000000000000")
        });
        numberingFormats.Count = UInt32Value.FromUInt32((uint)numberingFormats.ChildElements.Count);
        #endregion

        #region Fonts
        var fonts = new Fonts();
        fonts.Append(new Font()  // Font index 0 - default
        {
            FontName = new FontName { Val = StringValue.FromString("Calibri") },
            FontSize = new FontSize { Val = DoubleValue.FromDouble(11) }
        });
        fonts.Append(new Font()  // Font index 1
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
            NumberFormatId = 22,        // 22 = 'dd/mm/yyyy hh:mm' formato PREDEFINITO Excel
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
        cellFormats.Append(new CellFormat  // Cell format header  // Cell format index 8
        {
            NumberFormatId = 49,
            FontId = 1,
            FillId = 0,
            BorderId = 2,
            FormatId = 0,
            ApplyNumberFormat = BooleanValue.FromBoolean(true),
            Alignment = new Alignment() { WrapText = false, Horizontal = HorizontalAlignmentValues.Center }
        });
        // 
        cellFormats.Append(new CellFormat  // Cell format 14 digits  // Cell format index 9
        {
            NumberFormatId = DIGITS14_FORMAT,
            FontId = 0,
            FillId = 0,
            BorderId = 0,
            FormatId = 0,
            ApplyNumberFormat = BooleanValue.FromBoolean(true)
        });
        cellFormats.Count = UInt32Value.FromUInt32((uint)cellFormats.ChildElements.Count);
        #endregion


        // Append to stylesheet in correct order as per Office Open XML standard
        // Order: NumberingFormats -> Fonts -> Fills -> Borders -> CellStyleFormats -> CellFormats
        workbookstylesheet.Append(numberingFormats);
        workbookstylesheet.Append(fonts);
        workbookstylesheet.Append(fills);
        workbookstylesheet.Append(borders);
        workbookstylesheet.Append(cellStyleFormats);
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
        var sheet = wbPart.Workbook.Descendants<Sheet>().FirstOrDefault(s => s.Name == sheetName)!;
        return sheet != null;
    }
    public static bool IsValidDecimal(object input)
    {
        // Ensure the input is a string
        var inputStr = input!.ToString();

        // Check if the input can be parsed as an integer
        if (long.TryParse(inputStr, out _))
            return false;

        // Check if the input starts with a leading zero (but not just "0")
        if (inputStr!.Length > 1 && inputStr.StartsWith("0"))
            return false;

        // Try to parse as decimal
        if (decimal.TryParse(inputStr, out _))
            return true;

        return false;
    }

    /// <summary>
    /// PF-753: proprietà estesa di una <see cref="DataTable"/> che indica che l'ultima riga è quella dei
    /// totali (impostata da <c>FillTableWithTotalsRel</c>). <see cref="ToExcel"/> la scrive con
    /// <c>SUBTOTAL(9, …)</c>, così i totali seguono i filtri applicati in Excel.
    /// </summary>
    public const string SubtotalRowProperty = "SubtotalRow";

    /// <summary>PF-753: proprietà estesa di una <see cref="DataColumn"/> con la larghezza fissa (double) della colonna.</summary>
    public const string ColumnWidthProperty = "Width";

    /// <summary>
    /// PF-753: proprietà estesa di una <see cref="DataTable"/> che chiede il ridimensionamento automatico delle
    /// colonne sul contenuto. Open XML non ha un "autofit": la larghezza è stimata in <see cref="ToExcel"/> dal
    /// testo più lungo fra intestazione e celle (per i testi multiriga, la riga più lunga), con un massimo di
    /// <see cref="AutoFitMaxChars"/> caratteri. Oltre, la colonna va a capo se ha lo stile Wrapper.
    /// </summary>
    public const string AutoFitProperty = "AutoFit";

    /// <summary>
    /// PF-753: limite, in caratteri, della larghezza calcolata da <see cref="AutoFitProperty"/>. Un testo anomalo
    /// (es. una ragione sociale di centinaia di caratteri) non deve produrre una colonna enorme.
    /// </summary>
    public const int AutoFitMaxChars = 80;

    /// <summary>
    /// PF-753: numero di caratteri con cui una cella viene mostrata in Excel, usato per stimare la larghezza della
    /// colonna quando la tabella chiede <see cref="AutoFitProperty"/>. La stima segue il modo in cui
    /// <see cref="ToExcel"/> scrive il valore:
    /// <list type="bullet">
    /// <item>stringa: la riga più lunga (le note sono multiriga, separate da <c>\n</c>, eventualmente <c>\r\n</c>);</item>
    /// <item>decimale: il numero con separatore delle migliaia e due decimali, più due caratteri di margine
    /// per il simbolo di valuta o il segno;</item>
    /// <item>data: 19 caratteri, cioè <c>yyyy/MM/dd HH:mm:ss</c>;</item>
    /// <item>valore nullo: 0, così una colonna vuota resta larga quanto la sua intestazione.</item>
    /// </list>
    /// </summary>
    public static int DisplayLength(object? value) => value switch
    {
        null or DBNull => 0,
        // Multiriga: conta la riga più lunga, non la somma (la cella va a capo sulle altre righe).
        string s => s.Split('\n').Max(l => l.TrimEnd('\r').Length),
        decimal d => d.ToString("#,##0.00", System.Globalization.CultureInfo.InvariantCulture).Length + 2,
        double d => d.ToString("#,##0.00", System.Globalization.CultureInfo.InvariantCulture).Length + 2,
        DateTime => 19,
        _ => value.ToString()?.Length ?? 0,
    };

    /// <summary>
    /// PF-753: lettera Excel della colonna a partire dall'indice 0-based (0 → A, 25 → Z, 26 → AA, 701 → ZZ,
    /// 702 → AAA). Serve a comporre l'intervallo della formula <c>SUBTOTAL</c>. È la numerazione biiettiva in
    /// base 26: non esiste la "cifra zero", per questo si sottrae 1 a ogni passo.
    /// </summary>
    public static string ColumnLetter(int zeroBasedIndex)
    {
        var letters = string.Empty;
        var n = zeroBasedIndex + 1;
        while (n > 0)
        {
            var rem = (n - 1) % 26;
            letters = (char)('A' + rem) + letters;
            n = (n - 1) / 26;
        }
        return letters;
    }

    /// <summary>
    /// PF-753: cella del totale di una colonna scritta come <c>SUBTOTAL(9, X2:Xn)</c>. La funzione 9 è la somma
    /// che ignora le righe nascoste dai filtri, quindi il totale segue i filtri applicati in Excel. Il valore già
    /// calcolato viene scritto anche come cache (<see cref="CellValue"/>): chi legge il file senza ricalcolare le
    /// formule (librerie, anteprime di posta) vede lo stesso numero di prima.
    /// </summary>
    /// <param name="zeroBasedColumn">Indice 0-based della colonna nel foglio.</param>
    /// <param name="lastDataExcelRow">Ultima riga Excel con dati (la riga 1 è l'intestazione).</param>
    /// <param name="cachedValue">Totale già calcolato da <c>FillTableWithTotalsRel</c>.</param>
    /// <param name="styleIndex">Stile della colonna, lo stesso delle celle dati.</param>
    private static Cell SubtotalCell(int zeroBasedColumn, int lastDataExcelRow, decimal cachedValue, uint styleIndex)
    {
        var letter = ColumnLetter(zeroBasedColumn);
        return new Cell
        {
            DataType = CellValues.Number,
            CellFormula = new CellFormula($"SUBTOTAL(9,{letter}2:{letter}{lastDataExcelRow})"),
            CellValue = new CellValue(cachedValue),
            StyleIndex = styleIndex,
        };
    }

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

                // PF-753: ridimensionamento automatico sul contenuto, solo per le tabelle che lo chiedono
                // (es. sheet "Non Fatturate"). Per ogni colonna si parte dalla lunghezza dell'intestazione,
                // calcolata sopra, e la si allarga fino alla cella più lunga, senza superare AutoFitMaxChars.
                // Il limite si applica alle celle e non all'intestazione: una colonna non è mai più stretta
                // del proprio titolo. La conversione da caratteri a larghezza resta quella qui sotto.
                if (table.ExtendedProperties[AutoFitProperty] is true)
                {
                    for (var j = 0; j < table.Columns.Count; j++)
                    {
                        var max = numbersOfChars[j]!.Value;
                        foreach (DataRow r in table.Rows)
                            max = Math.Max(max, Math.Min(DisplayLength(r[j]), AutoFitMaxChars));
                        numbersOfChars[j] = max;
                    }
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
                    var width = Math.Truncate(((widthPixels - 5f) / maxDigitFont * 100f + 0.5f) / 100f) + 5;
                    // PF-753: larghezza esplicita (es. colonna "Descrizione" della Legenda), altrimenti
                    // calcolata dalla sola intestazione come prima.
                    if (column.ExtendedProperties[ColumnWidthProperty] is double fixedWidth)
                        width = fixedWidth;
                    var cln = new Column
                    {
                        Min = Convert.ToUInt32(index + 1),
                        Max = Convert.ToUInt32(index + 1),
                        Width = width,
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

                // PF-753: la riga "Totali:" prodotta da FillTableWithTotalsRel è l'ultima della tabella,
                // preceduta da una riga vuota; le righe dati vanno quindi dalla riga Excel 2 alla (righe - 2) + 1.
                var hasSubtotalRow = table.ExtendedProperties[SubtotalRowProperty] is true;
                var dataRowCount = table.Rows.Count - 2;
                var lastDataExcelRow = dataRowCount + 1;

                var rowIndex = 0;
                foreach (DataRow dsrow in table.Rows)
                {
                    rowIndex++;
                    // Solo l'ultima riga di una tabella marcata, e solo se ci sono righe dati: senza dati
                    // l'intervallo sarebbe "X2:X1" (comprenderebbe l'intestazione), quindi restano i valori statici.
                    var isSubtotalRow = hasSubtotalRow && dataRowCount > 0 && rowIndex == table.Rows.Count;
                    var newRow = new Row();
                    var colIndex = -1;
                    foreach (var col in columns)
                    {
                        colIndex++;
                        // Nella riga dei totali: la prima colonna è l'etichetta "Totali:"; le colonne numeriche
                        // (decimal o int, cioè quelle che FillTableWithTotalsRel ha sommato) diventano SUBTOTAL;
                        // le altre (DBNull, testi) passano dal ramo normale qui sotto.
                        if (isSubtotalRow && colIndex > 0 && dsrow[col.Key] is decimal or int)
                        {
                            newRow.AppendChild(SubtotalCell(colIndex, lastDataExcelRow, Convert.ToDecimal(dsrow[col.Key]), Convert.ToUInt32(col.Value)));
                            continue;
                        }

                        var (cellType, value, type) = TypeFinder.Get(dsrow[col.Key]!);

                        CellValue? cellvalue = null;
                        var cellvalues = cellType;
                        var styleIndex = Convert.ToUInt32(col.Value);

                        if (value == null)
                            cellvalue = new CellValue(string.Empty);
                        else if (type == typeof(long))
                            cellvalue = new CellValue(Convert.ToDecimal(value));
                        else if (type == typeof(string) && IsValidDecimal(value))
                        {
                            cellvalues = CellValues.Number;
                            styleIndex = 3;
                            cellvalue = new CellValue(Convert.ToDecimal(value));
                        }
                        else if (type == typeof(DateTime))
                        {
                            cellvalues = CellValues.String;
                            cellvalue = new CellValue(((DateTime)value).ToString("yyyy/MM/dd HH:mm:ss"));
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

    public static MemoryStream ToExcelData(this DataSet ds)
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
                        else if (type == typeof(long))
                            cellvalue = new CellValue(Convert.ToDecimal(value));
                        else if (type == typeof(DateTime))
                        {
                            cellvalues = CellValues.Date;
                            cellvalue = new CellValue(((DateTime)value).ToString("yyyy-MM-dd"));
                            styleIndex = 2;
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

    public static DataTable ReadAsseverazioneExcel(this MemoryStream stream)
    {
        var table = new DataTable();
        var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet(1);
        table.Columns.Add(0.ToString());
        table.Columns.Add(1.ToString());
        table.Columns.Add(2.ToString());
        table.Columns.Add(3.ToString());
        table.Columns.Add(4.ToString());
        for (var i = 0; i < ws.Rows().Count(); i++)
        {
            var tempRow = table.NewRow();
            tempRow[0] = ws.Cell($"A{i + 1}").Value;
            tempRow[1] = ws.Cell($"B{i + 1}").Value;
            tempRow[2] = ws.Cell($"C{i + 1}").Value;
            tempRow[3] = ws.Cell($"D{i + 1}").Value;
            tempRow[4] = ws.Cell($"E{i + 1}").Value;
            table.Rows.Add(tempRow);
        }
        table.Rows.RemoveAt(0);
        return table;
    }
}