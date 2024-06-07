using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;

namespace PortaleFatture.BE.Api.Infrastructure.Documenti;

public static class DocsExtensions
{
    private static readonly string _mimeCsv = "text/csv";
    private static readonly string _mimeExcel = "application/vnd.ms-excel";
    private static readonly CsvConfiguration _csvConfiguration = new(new CultureInfo("it-IT"))
    {
        Delimiter = ";",
        HasHeaderRecord = true
    };


    private static async Task<IResult> ToFile<T, M>(this IEnumerable<T> lista) where M : ClassMap<T>
    {
        var filename = $"{Guid.NewGuid()}.csv";
        byte[] data;
        using (var stream = new MemoryStream())
        {
            using (TextWriter textWriter = new StreamWriter(stream))
            {
                using var csv = new CsvWriter(textWriter, _csvConfiguration);
                csv.Context.RegisterClassMap<M>();
                stream.Position = 0;
                await csv.WriteRecordsAsync(lista!);
                await textWriter.FlushAsync();
                data = stream.ToArray();
                await csv.FlushAsync();
            }
            await stream.FlushAsync();
        }
        return Results.File(data!, _mimeCsv, filename);
    }

    public static async Task<IResult> ToCsv<T, M>(this IEnumerable<T> lista) where M : ClassMap<T>
    {
        return await ToFile<T, M>(lista);
    }

    public static async  Task<FileStreamResult> ToCsvv2<T, M>(this IEnumerable<T> lista) where M : ClassMap<T>
    {
        var filename = $"{Guid.NewGuid()}.csv";
        var stream = new MemoryStream();

        using (var textWriter = new StreamWriter(stream, leaveOpen: true))
        using (var csv = new CsvWriter(textWriter, _csvConfiguration))
        {
            csv.Context.RegisterClassMap<M>();
            await csv.WriteRecordsAsync(lista);
            await textWriter.FlushAsync(); 
        } 

        stream.Seek(0, SeekOrigin.Begin); 

        return new FileStreamResult(stream, _mimeCsv)
        {
            FileDownloadName = filename
        };
    }

    public static async Task<IResult> ToExcel<T>(this IEnumerable<T> lista)
    {
        var filename = $"{Guid.NewGuid()}.xlsx";
        var dataSet = lista.FillOneSheet();
        byte[] data;
        using (var stream = dataSet.ToExcel())
        {
            data = stream.ToArray();
            await stream.FlushAsync();
        }
        return Results.File(data!, _mimeExcel, filename);
    }

    public static async Task<IResult> ToExcelv2<T>(this IEnumerable<T> lista)
    {
        var filename = $"{Guid.NewGuid()}.xlsx";
        var dataSet = lista.FillOneSheetv2();
        byte[] data;
        using (var stream = dataSet.ToExcel())
        {
            data = stream.ToArray();
            await stream.FlushAsync();
        }
        return Results.File(data!, _mimeExcel, filename);
    }
}