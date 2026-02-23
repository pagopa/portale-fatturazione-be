using Microsoft.Extensions.Configuration;
using PortaleFatture.BE.ImportFattureGrandiAderenti.Extensions;
using PortaleFatture.BE.ImportFattureGrandiAderenti.Models;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var env = config["Env"];
var sqlServer = config[$"ConnectionStrings:SqlServer{env}"];
var database = config[$"ConnectionStrings:Database{env}"];


var connection = SQLHelper.GetConnection(sqlServer!, database!);

await connection.OpenAsync();


var tipologiaFatturaPrimoSaldo = "primo saldo";
var baseDirectory = AppContext.BaseDirectory;
var dataFolderPath = Path.Combine(baseDirectory, "data", "fatture");
var subDirectories = Directory.GetDirectories(dataFolderPath);

var listFatture = await subDirectories.GetFile(tipologiaFatturaPrimoSaldo);
object FkIdDatiFatturazione = DBNull.Value;  

using (var transaction = await connection.BeginTransactionAsync())
{
    foreach (var fattura in listFatture)
    {
        foreach (var f in fattura.listaFatture!)
        {
            static bool ShouldSubtract(Posizione x) => x.codiceMateriale!.Contains("STORNO");
            var command = connection.CreateCommand();
            command.Transaction = (Microsoft.Data.SqlClient.SqlTransaction)transaction;
            // trova FkIdDatiFatturazione 
            command.CommandText = ImportExtensions.SQLSelectIdDatiFatturazione(); 
            command.Parameters.AddWithValue("@FkIdEnte", f.fattura!.istitutioID); 
            var fkIdDatiFatturazione = command.ExecuteScalar();

            if (fkIdDatiFatturazione != null)
                FkIdDatiFatturazione = Convert.ToInt32(fkIdDatiFatturazione);
            else
                FkIdDatiFatturazione = DBNull.Value;

            var totaleFattura = f.fattura.posizioni!.Sum(x => ShouldSubtract(x) ? -x.imponibile : x.imponibile);
            command.CommandText = ImportExtensions.SQLInsertTestata();
            command.Parameters.AddWithValue("@FkProdotto", "prod-pn");
            command.Parameters.AddWithValue("@DataFattura", f.fattura!.dataFattura);
            command.Parameters.AddWithValue("@FkIdTipoDocumento", f.fattura.tipoDocumento);
            command.Parameters.AddWithValue("@FkTipologiaFattura", f.fattura.tipologiaFattura);
            //command.Parameters.AddWithValue("@FkIdEnte", f.fattura.istitutioID); //già aggiunto
            command.Parameters.AddWithValue("@FkIdDatiFatturazione", FkIdDatiFatturazione);
            command.Parameters.AddWithValue("@IdentificativoFattura", $"prod-pn-{f.fattura.numero}");
            command.Parameters.AddWithValue("@TotaleFattura", f.fattura.tipoDocumento == "TD04"? totaleFattura*-1: totaleFattura); 
            command.Parameters.AddWithValue("@Divisa", f.fattura.divisa);
            command.Parameters.AddWithValue("@MetodoPagamento", f.fattura.metodoPagamento);
            command.Parameters.AddWithValue("@AnnoRiferimento", f.fattura.identificativo!.Split("/")[1]);
            command.Parameters.AddWithValue("@MeseRiferimento", f.fattura.identificativo!.Split("/")[0]);
            command.Parameters.AddWithValue("@CausaleFattura", f.fattura.causale);
            command.Parameters.AddWithValue("@Sollecito", f.fattura.sollecito);
            command.Parameters.AddWithValue("@CodiceContratto", f.fattura.onboardingTokenID);
            command.Parameters.AddWithValue("@SplitPayment", f.fattura.split); 
            var datiGenerali = f.fattura.datiGeneraliDocumento?.FirstOrDefault();
 
            command.Parameters.AddWithValue("@Cup", datiGenerali?.CUP ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Cig", datiGenerali?.CIG ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IdDocumento", datiGenerali?.idDocumento ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DataDocumento", datiGenerali?.data ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@NumItem", datiGenerali?.numItem ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CodCommessa", datiGenerali?.codiceCommessaConvenzione ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Progressivo", f.fattura.numero);

            var fkIdFattura = Convert.ToInt32(await command.ExecuteScalarAsync());
            if (fkIdFattura > 0)
            {
                var row_affected = 0;
                foreach (var pos in f.fattura.posizioni!)
                {
                    command.CommandText = ImportExtensions.SQLInsertRiga();
                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@FkIdFattura", fkIdFattura);
                    command.Parameters.AddWithValue("@NumeroLinea", pos.numerolinea);
                    command.Parameters.AddWithValue("@Testo", pos.testo);
                    command.Parameters.AddWithValue("@CodiceMateriale", pos.codiceMateriale);
                    command.Parameters.AddWithValue("@Quantita", pos.quantita);
                    command.Parameters.AddWithValue("@PrezzoUnitario", pos.prezzoUnitario);
                    command.Parameters.AddWithValue("@Imponibile", pos.imponibile);
                    command.Parameters.AddWithValue("@RigaBollo", 0);
                    command.Parameters.AddWithValue("@PeriodoRiferimento", pos.periodoRiferimento);
                    row_affected += await command.ExecuteNonQueryAsync();
                }
                Console.WriteLine($"Fattura {f.fattura.identificativo} inserita correttamente.");
            }
            else
            {
                Console.WriteLine($"Errore nell'inserimento della fattura {f.fattura.identificativo}.");
            }
        }
    }
    await transaction.CommitAsync();
}
await connection.CloseAsync();

Console.ReadKey(true);