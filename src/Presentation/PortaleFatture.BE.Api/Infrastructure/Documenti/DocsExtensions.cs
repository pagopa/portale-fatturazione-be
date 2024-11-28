using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

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

    [Obsolete("This method is obsolete. Use ToStream instead.", false)]
    public static async Task<byte[]?> ToArray<T, M>(this IEnumerable<T> lista) where M : ClassMap<T>
    {
        byte[]? data;
        using (var stream = new MemoryStream())
        {
            using TextWriter textWriter = new StreamWriter(stream);
            using var csv = new CsvWriter(textWriter, _csvConfiguration);
            csv.Context.RegisterClassMap<M>();
            stream.Position = 0;
            await csv.WriteRecordsAsync(lista!);
            await textWriter.FlushAsync();
            await stream.FlushAsync();
            data = stream.ToArray();
            await csv.FlushAsync();
        }
        return data;
    }

    public static async Task<Stream> ToStream<T, M>(this IEnumerable<T> lista) where M : ClassMap<T>
    {
        var stream = new MemoryStream();

        using (var textWriter = new StreamWriter(stream, leaveOpen: true))
        using (var csv = new CsvWriter(textWriter, _csvConfiguration))
        {
            csv.Context.RegisterClassMap<M>();
            await csv.WriteRecordsAsync(lista);
            await textWriter.FlushAsync();
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    [Obsolete("This method is obsolete. Use ToStream instead.", false)]
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
        return Results.File(data!, _mimeCsv, filename, enableRangeProcessing: true);
    }

    [Obsolete("This method is obsolete.", false)]
    public static void ForceGarbageCollection()
    {
        // Force a full collection of all generations
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        // Wait for pending finalizers to complete
        GC.WaitForPendingFinalizers();

        // Optionally, force another collection to reclaim objects that became unreachable due to finalizers
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    [Obsolete("This method is obsolete.", false)]
    public static async Task<IResult> ToCsv<T, M>(this IEnumerable<T> lista) where M : ClassMap<T>
    {
        return await ToFile<T, M>(lista);
    }

    [Obsolete("This method is obsolete.", false)]
    public static async Task<FileStreamResult> ToCsvv2<T, M>(this IEnumerable<T> lista) where M : ClassMap<T>
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

    [Obsolete("This method is obsolete.", false)]
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

    [Obsolete("This method is obsolete.", false)]
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