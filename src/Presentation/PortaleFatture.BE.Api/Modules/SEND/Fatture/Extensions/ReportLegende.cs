using System.Data;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;

/// <summary>
/// PF-753: legende statiche dei report Excel dei documenti emessi e sospesi (documento "Legende dei report Excel").
/// Ogni report riceve come primo sheet "Legenda", con una riga per ciascun foglio previsto per la sua tipologia,
/// anche se nel singolo file non viene generato. La legenda non dipende dai dati: cambia solo il mese nei nomi.
/// <c>{mese}</c> è sostituito con il mese del report; senza mese (report delle non inviate) il nome resta quello
/// che il report scrive davvero, normalizzato negli spazi.
/// </summary>
public static class ReportLegende
{
    public const string SheetName = "Legenda";
    public const string ColonnaNomeSheet = "Nome sheet";
    public const string ColonnaDescrizione = "Descrizione";
    public const string DescrizioneLegenda = "Elenco dei fogli del report e descrizione del relativo contenuto";

    /// <summary>PF-1029: foglio del report Quadrature Regolare Esecuzione (<c>api/rel/pagopa/quadratura/ricerca</c>).
    /// Prima si chiamava "T", il nome di default di <c>ToTablev2</c>.</summary>
    public const string SheetQuadratureRel = "Quadrature Reg Esecuzione";

    /// <summary>PF-1029: foglio del report Regolari Esecuzioni non fatturate (<c>api/rel/pagopa/nonfatturate</c>).
    /// Prima si chiamava "T".</summary>
    public const string SheetRelNonFatturate = "Reg Esecuzioni non fatturate";

    /// <summary>Riga della legenda: nome del foglio (con il segnaposto <c>{mese}</c>) e descrizione del contenuto.</summary>
    public sealed record Voce(string NomeSheet, string Descrizione);

    // Testi: documento "Legende dei report Excel" (LegendeReport_BE.md) aggiornato il 20/11/2026, riportati as-is
    // (PF-1029, voce 10 della checklist). Il nome dello sheet di quadratura segue invece la checklist (v. SheetQuadratureRel).

    /// <summary>Fatture emesse di PRIMO SALDO, SECONDO SALDO, VAR. SEMESTRALE, SEM. SOSPESI (anche per le non inviate).</summary>
    public static readonly IReadOnlyList<Voce> FattureEmesse =
    [
        new("Regolari Esecuzioni {mese}", "REL associate alle fatture, della tipologia del report del mese di riferimento del report."),
        new("Reg. Esec. PAC non firmate", "REL degli enti PAC che non hanno firmato e caricato la REL, del mese di riferimento del report."),
        new("Enti Fatt. {mese}", "Fatture emesse, della tipologia del report, con imponibile diverso da zero del mese di riferimento del report."),
        new("Enti Fatt. a Zero {mese}", "Fatture, della tipologia del report, con imponibile pari a zero del mese di riferimento del report."),
        new("Enti Fatt. {mese} Sospesi", "Fatture sospese e imponibile diverso da zero, del mese di riferimento del report."),
        new("Non Fatturate", "Mostra le fatture, della tipologia del report, posticipate di qualsiasi periodo; mostra le fatture eliminate del solo mese di riferimento del report."),
    ];

    /// <summary>PF-1029: descrizione dello sheet di quadratura (report Quadrature Regolare Esecuzione e sheet del PRIMO SALDO).</summary>
    public const string DescrizioneQuadratureRel = "Mostra, per il mese di riferimento, i valori imponibili e conteggio delle contestazioni, delle REL, dell'Asseverazione e infine il consuntivo; in ultimo le colonne di check per la differenza di imponibile e conteggio.";

    /// <summary>
    /// PF-1029: fatture emesse di PRIMO SALDO nel report per periodo: come <see cref="FattureEmesse"/>, più lo sheet
    /// "Quadrature Reg Esecuzione" dopo le REL. Il report delle non inviate (senza periodo) resta su <see cref="FattureEmesse"/>.
    /// </summary>
    public static readonly IReadOnlyList<Voce> FattureEmessePrimoSaldo =
    [
        FattureEmesse[0],
        FattureEmesse[1],
        new(SheetQuadratureRel, DescrizioneQuadratureRel),
        .. FattureEmesse.Skip(2),
    ];

    /// <summary>Fatture emesse di ANTICIPO. Dal PF-753 il report non produce più gli sheet "stimate" ed "eliminate".</summary>
    public static readonly IReadOnlyList<Voce> Anticipo =
    [
        new("Com. {mese}", "Modulo Commessa ricevuti e in stato Chiusa/Caricato per il mese di riferimento del report."),
        new("Com. fatt. {mese}", "Moduli Commessa ricevuti ed utilizzati per il calcolo delle fatture di Anticipo del mese di riferimento."),
        new("Com. non fatt. {mese}", "Moduli Commessa ricevuti senza fattura di Anticipo associata per il mese di riferimento."),
        new("Non Fatturate {mese}", "Mostra le fatture di Anticipo posticipate di qualsiasi periodo; mostra le fatture di Anticipo eliminate del solo mese di riferimento del report."),
    ];

    /// <summary>Fatture emesse di ACCONTO.</summary>
    public static readonly IReadOnlyList<Voce> Acconto =
    [
        new("Acconto {mese}", "Fatture di Acconto con totale diverso da zero per il mese di riferimento del report."),
        new("Acconto a zero {mese}", "Fatture di Acconto con totale pari a zero per il mese di riferimento del report."),
        new("Non Fatturate {mese}", "Mostra le fatture di Acconto posticipate di qualsiasi periodo; mostra le fatture di Acconto eliminate del solo mese di riferimento del report."),
    ];

    /// <summary>Fatture sospese di PRIMO SALDO, SECONDO SALDO, VAR. SEMESTRALE, SEM. SOSPESI.</summary>
    public static readonly IReadOnlyList<Voce> FattureSospese =
    [
        new("Reg. Esec. Sospese {mese}", "REL associate alle fatture sospese, della tipologia del report del mese di riferimento del report."),
        new("Enti Fatt. Sospese {mese}", "Fatture emesse, della tipologia del report, con imponibile diverso da zero del mese di riferimento del report."),
        new("Fatt. a Zero Sosp. {mese}", "Fatture, della tipologia del report, con imponibile pari a zero del mese di riferimento del report."),
        new("Dett Fatt Sosp {mese}", "Mostra il dettaglio delle posizioni delle fatture sospese del mese di riferimento del report."),
    ];

    /// <summary>Fatture sospese di ANTICIPO (stessa rimozione degli sheet dell'anticipo emesso, confermata il 20/11/2026).</summary>
    public static readonly IReadOnlyList<Voce> AnticipoSospeso =
    [
        new("Com. Sospese {mese}", "Modulo Commessa ricevuti e in stato Chiusa/Caricato associati alle fatture di Anticipo sospese del mese di riferimento."),
        new("Com. fatt. Sospese {mese}", "Moduli Commessa ricevuti ed utilizzati per il calcolo delle fatture di Anticipo sospese del mese di riferimento."),
        new("Com. non fatt. Sosp. {mese}", "Moduli Commessa ricevuti senza fattura di Anticipo sospesa associata del mese di rifermento del report."),
    ];

    /// <summary>Fatture sospese di ACCONTO; usata anche dal report delle non inviate per l'acconto.</summary>
    public static readonly IReadOnlyList<Voce> AccontoSospeso =
    [
        new("Acconto Sospesi {mese}", "Fatture di Acconto sospese con totale diverso da zero."),
        new("Acconto a zero Sosp. {mese}", "Fatture di Acconto sospese con totale pari a zero."),
        new("Non Fatturate {mese}", "Fatture di Acconto posticipate o eliminate presenti in Gestione Fatture."),
    ];

    /// <summary>PF-1029: sheet del report Andamento Credito Sospeso (<c>api/fatture/andamento-sospese/download</c>).</summary>
    public const string SheetAndamentoCreditoSospeso = "Andamento Credito Sosp.";

    /// <summary>PF-1029: report Andamento Credito Sospeso.</summary>
    public static readonly IReadOnlyList<Voce> AndamentoCreditoSospeso =
    [
        new(SheetAndamentoCreditoSospeso, "Mostra per ogni ente, che ha almeno una fattura sospesa, il valore di ogni fattura sospesa e del credito cumulato fino al momento del download del report."),
    ];

    /// <summary>PF-1029: report Quadrature Regolare Esecuzione.</summary>
    public static readonly IReadOnlyList<Voce> QuadratureRel =
    [
        new(SheetQuadratureRel, DescrizioneQuadratureRel),
    ];

    /// <summary>PF-1029: report Regolari Esecuzioni Non Fatturate.</summary>
    public static readonly IReadOnlyList<Voce> RelNonFatturate =
    [
        new(SheetRelNonFatturate, "Mostra, per qualsiasi periodo e tipologia, i valori imponibili delle REL e relativo stato di Firma, se la REL è firmata mostra la data del caricamento."),
    ];

    /// <summary>Nome del foglio con il mese sostituito e gli spazi normalizzati.</summary>
    public static string NomeSheet(string modello, string? month)
        => string.Join(' ', modello.Replace("{mese}", month ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Normalizza gli spazi nei nomi dei fogli del report (spazi doppi o in coda), con la stessa regola di
    /// <see cref="NomeSheet"/>. Serve al report delle non inviate, che non ha mese: i builder compongono nomi come
    /// <c>$"Regolari Esecuzioni {month}"</c> e con <c>month</c> vuoto il foglio si chiamerebbe
    /// <c>"Regolari Esecuzioni "</c> (o <c>"Enti Fatt.  Sospesi"</c>), diverso dalla voce della legenda.
    /// Va chiamato subito prima di <c>ToExcel</c>, quando tutti i fogli sono stati aggiunti.
    /// </summary>
    /// <remarks>Rinomina solo se il nome cambia; due fogli che diventassero uguali farebbero fallire il rename
    /// (<see cref="DuplicateNameException"/>), che è preferibile a un file con un foglio sovrascritto.</remarks>
    public static DataSet NormalizzaNomiSheet(this DataSet dataSet)
    {
        foreach (DataTable table in dataSet.Tables)
        {
            var nome = NomeSheet(table.TableName, null);
            if (nome != table.TableName)
                table.TableName = nome;
        }
        return dataSet;
    }

    /// <summary>
    /// Aggiunge lo sheet "Legenda". Va chiamato sul DataSet ancora vuoto, perché la legenda deve essere il
    /// primo foglio e <see cref="DataTableCollection"/> non permette di inserire in testa.
    /// </summary>
    public static DataSet AddLegenda(this DataSet dataSet, IReadOnlyList<Voce> voci, string? month)
    {
        if (dataSet.Tables.Count > 0)
            throw new InvalidOperationException("La Legenda deve essere il primo sheet del report.");

        var table = new DataTable(SheetName);
        table.Columns.Add(Colonna(ColonnaNomeSheet, XCellStyle.None, 35d));
        table.Columns.Add(Colonna(ColonnaDescrizione, XCellStyle.Wrapper, 100d));

        table.Rows.Add(SheetName, DescrizioneLegenda);
        foreach (var voce in voci)
            table.Rows.Add(NomeSheet(voce.NomeSheet, month), voce.Descrizione);

        dataSet.Tables.Add(table);
        return dataSet;
    }

    // Colonna di testo della legenda con stile e larghezza fissa (la larghezza è letta da ExcelExtensions.ToExcel).
    private static DataColumn Colonna(string nome, XCellStyle style, double width)
    {
        var column = new DataColumn(nome, typeof(string)) { Caption = nome };
        column.ExtendedProperties.Add("Style", style);
        column.ExtendedProperties.Add(ExcelExtensions.ColumnWidthProperty, width);
        return column;
    }
}
