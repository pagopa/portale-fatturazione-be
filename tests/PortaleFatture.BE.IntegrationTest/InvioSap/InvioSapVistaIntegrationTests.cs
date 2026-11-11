namespace PortaleFatture.BE.IntegrationTest.InvioSap;

/// <summary>
/// Contratto con DATA sulle quattro viste dell'Invio a SAP, sul seed 2003. Ogni caso fissa un comportamento
/// osservato in DEV e confermato da DATA (C1-C14, be-invio-sap.md §5.4): se DATA cambia una vista, il test
/// diventa rosso con il rimando al paragrafo, e il BE va riallineato consapevolmente.
/// </summary>
[Category("InvioSap")]
[NonParallelizable]
public class InvioSapVistaIntegrationTests
{
    private const string Rif = "v. be-invio-sap.md §5.4";

    [OneTimeSetUp]
    public void Setup()
    {
        TestDb.SkipIfUnavailable(LocalTestDb.ConnectionString);
        InvioSapTestData.SkipSeOggettiAssenti();
        InvioSapTestData.Pulisci();
    }

    private static List<long> IdNelDettaglio() =>
        InvioSapTestData.Lista<long>($"SELECT IdFattura FROM {InvioSapTestData.VistaDettaglio} WHERE AnnoRiferimento = 2003 ORDER BY IdFattura");

    [Test]
    public void Colonne_DelleViste_ShouldEssereQuelleUsateDalBackend()
    {
        string Colonne(string vista) => string.Join(",", InvioSapTestData.Lista<string>(
            "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(@v) ORDER BY column_id", ("@v", vista)));
        Assert.Multiple(() =>
        {
            Assert.That(Colonne(InvioSapTestData.VistaGriglia), Is.EqualTo("TipologiaFattura,StatoFattura,NumeroFatture,Anno,Mese,Importo Totale"));
            Assert.That(Colonne(InvioSapTestData.VistaDettaglio), Is.EqualTo(
                "IdFattura,FkIdTipoDocumento,FkTipologiaFattura,FkIdEnte,RagioneSociale,DataFattura,IdentificativoFattura,TotaleFattura," +
                "AnnoRiferimento,MeseRiferimento,CausaleFattura,CodiceContratto,IdDocumento,DataDocumento,Progressivo,FatturaInviata," +
                "Semestre,FaseFatturazione,DataUltimaGenerazione,NomeFile,NoteInvio"));
            Assert.That(Colonne(InvioSapTestData.VistaReinviate), Is.EqualTo(
                "IdFattura,TipoDocumento,TipologiaFattura,IdEnte,RagioneSociale,DataFattura,TotaleFattura,AnnoRiferimento," +
                "MeseRiferimento,Progressivo,ConteggioInvio,Note"));
            Assert.That(Colonne(InvioSapTestData.VistaLock), Is.EqualTo("IdAzione,Azione,LockStartUTC,LockTimeoutUTC,BatchId"));
        });
    }

    [Test]
    public void C1_FattureMaiInviate_ShouldEssereNelDettaglio_SenzaStorico()
    {
        // Vista 1.1 (11/11/2026): LEFT JOIN sullo storico, quindi anche le mai inviate. 9510 manca perche' il suo
        // ente non e' in pfd.Enti (INNER JOIN): in griglia c'e', nel dettaglio no.
        var ids = IdNelDettaglio();
        var senzaStorico = InvioSapTestData.Lista<long>($@"SELECT IdFattura FROM {InvioSapTestData.VistaDettaglio}
            WHERE AnnoRiferimento = 2003 AND DataUltimaGenerazione IS NULL AND NomeFile IS NULL AND NoteInvio IS NULL ORDER BY IdFattura");
        Assert.Multiple(() =>
        {
            Assert.That(ids, Is.EqualTo(new long[] { 9501, 9502, 9503, 9504, 9505, 9506, 9507, 9508, 9509 }), $"C1, {Rif}");
            Assert.That(senzaStorico, Is.EqualTo(new long[] { 9505, 9506, 9508, 9509 }), $"C1: le mai inviate non hanno file, data ne' note, {Rif}");
        });
    }

    [Test]
    public void C2_Griglia_ShouldAvereUnRigoPerStato_ConNullPerInElaborazione()
    {
        var righi = InvioSapTestData.Lista<string>($@"SELECT CONCAT(ISNULL(CAST(StatoFattura AS varchar), 'NULL'), ':', NumeroFatture)
            FROM {InvioSapTestData.VistaGriglia} WHERE Anno = 2003 AND Mese = 2 AND TipologiaFattura = 'PRIMO SALDO' ORDER BY StatoFattura");
        Assert.That(righi, Is.EqualTo(new[] { "NULL:1", "0:2", "1:1" }), $"C2: 0 da inviare, 1 inviata, NULL in elaborazione, {Rif}");
    }

    [Test]
    public void C3_C13_Reinviate_ShouldContareSoloIBatchCompletati()
    {
        var conteggio = InvioSapTestData.Scalar($"SELECT ConteggioInvio FROM {InvioSapTestData.VistaReinviate} WHERE IdFattura = 9502");
        var note = InvioSapTestData.Scalar($"SELECT Note FROM {InvioSapTestData.VistaReinviate} WHERE IdFattura = 9502") as string;
        var migrata = InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.VistaReinviate} WHERE IdFattura = 9501");
        Assert.Multiple(() =>
        {
            Assert.That(conteggio, Is.EqualTo(2), $"C13: 9001 e 9004 COMPLETATO; 9003 in ERRORE non conta, {Rif}");
            Assert.That(Core.Entities.SEND.Fatture.InvioSap.InvioSapNota.ParseNoteInvio(note), Has.Count.EqualTo(3), $"C13: le note includono il batch in errore, {Rif}");
            Assert.That(migrata, Is.EqualTo(0), $"C3: una sola migrazione non e' un re-invio, {Rif}");
        });
    }

    [Test]
    public void C4_C6_UltimaGenerazione_ShouldEssereIlBatchCompletatoPiuRecente()
    {
        var nome9502 = InvioSapTestData.Scalar($"SELECT NomeFile FROM {InvioSapTestData.VistaDettaglio} WHERE IdFattura = 9502");
        var nome9504 = InvioSapTestData.Scalar($"SELECT NomeFile FROM {InvioSapTestData.VistaDettaglio} WHERE IdFattura = 9504");
        var data9502 = (DateTime?)InvioSapTestData.Scalar($"SELECT DataUltimaGenerazione FROM {InvioSapTestData.VistaDettaglio} WHERE IdFattura = 9502");
        Assert.Multiple(() =>
        {
            Assert.That(data9502, Is.EqualTo(new DateTime(2003, 4, 3, 9, 5, 0)), $"C4: il batch 9003 in ERRORE ha data NULL e va in fondo, {Rif}");
            Assert.That(nome9502, Is.EqualTo(nome9504), $"C6: due batch diversi possono avere lo stesso NomeFile, {Rif}");
        });
    }

    [Test]
    public void C9_LockScaduto_ShouldMancareDallaVista_MaRestareInTabella()
    {
        Assert.Multiple(() =>
        {
            Assert.That(InvioSapTestData.Scalar($"SELECT COUNT(*) FROM {InvioSapTestData.VistaLock}"), Is.EqualTo(0), $"C9, {Rif}");
            Assert.That(InvioSapTestData.BatchDelLock(), Is.EqualTo(InvioSapTestData.BatchErrore), "La riga resta finche' DATA non la cancella.");
        });
    }

    [Test]
    public void C11_IdDocumentoVuoto_ShouldArrivareComeStringaVuota()
    {
        Assert.That(InvioSapTestData.Scalar($"SELECT IdDocumento FROM {InvioSapTestData.VistaDettaglio} WHERE IdFattura = 9503"), Is.EqualTo(string.Empty),
            $"C11: il BE la converte in null nella response, {Rif}");
    }

    [Test]
    public void C14_Griglia_ShouldIncludereLEntePagoPA()
    {
        Assert.That(InvioSapTestData.Scalar($"SELECT NumeroFatture FROM {InvioSapTestData.VistaGriglia} WHERE Anno = 2003 AND Mese = 3 AND TipologiaFattura = 'ACCONTO'"),
            Is.EqualTo(1), $"C14: la griglia non filtra l'ente PagoPA, {Rif}");
    }
}
