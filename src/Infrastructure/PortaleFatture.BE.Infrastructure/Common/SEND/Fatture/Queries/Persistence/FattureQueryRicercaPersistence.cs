using System.Data;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence.Builder;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence.Builder;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries.Persistence;

public class FattureQueryRicercaPersistence(FattureQueryRicerca command) : DapperBase, IQuery<FattureListaDto?>
{
    private readonly FattureQueryRicerca _command = command;
    private static readonly string _sqlSelectEnti = EnteSQLBuilder.SelectAll();
    public async Task<FattureListaDto?> Execute(IDbConnection? connection, string schema, IDbTransaction? transaction, CancellationToken cancellationToken = default)
    {
        var computedFatture = new List<FatturaDto>();

        var anno = _command.Anno;
        var mese = _command.Mese;
        var tipoFattura = _command.TipologiaFattura;

        // Scelta ramo (EMESSE vs NON FATTURATE) + risoluzione del filtro tipologia sulla colonna giusta:
        // logica pura estratta nel builder (unit-testabile). Vedi FattureQueryRicercaBuilderTests.
        var sqlFatture = FattureQueryRicercaBuilder.SelectFattureRicerca(_command.Cancellata, !tipoFattura.IsNullNotAny());
        var sqlEnti = _sqlSelectEnti.Add(schema);

        var sql = string.Join(";", sqlEnti, sqlFatture);
        
        var query = new
        {
            AnnoRiferimento = anno,
            MeseRiferimento = mese,
            TipologiaFattura = tipoFattura,
            FkIdTipoContratto = _command.FkIdTipoContratto,
            FatturaInviata = _command.FatturaInviata
        };

        using var values = await ((IDatabase)this).QueryMultipleAsync<FattureListaDto>(
            connection!,
            sql,
            query,
            transaction);

        var enti = await values.ReadAsync<EnteContrattoDto>();
        var fatture = await values.ReadFirstAsync<FattureListaDto>();

        if (!_command.IdEnti!.IsNullNotAny())
            enti = enti.Where(x => _command.IdEnti!.Contains(x.IdEnte)).ToList();

        foreach (var f in fatture)
        {
            // Match ente case-insensitive: SQL Server confronta i GUID case-insensitive, quindi la vista
            // (be.vwDocumentiEmessiNonFatturati) puo' restituire un IstitutioID con casing diverso da
            // pfd.Enti (es. FkIdEnte maiuscolo in cfg.GestioneFatture). Un '==' case-sensitive scartava la
            // riga -> lista vuota -> 404. Vedi FattureRicercaApiIntegrationTests.NonFatturate_CasingEnteDiverso.
            var ente = enti.FirstOrDefault(x => string.Equals(x.IdEnte, f.fattura!.IstitutioID, StringComparison.OrdinalIgnoreCase));
            if (ente != null)
            {
                computedFatture.Add(f);
                f.fattura!.RagioneSociale = ente.RagioneSociale;
                f.fattura!.TipoContratto = ente.TipoContratto;
                f.fattura!.IdContratto = ente.IdContratto;

                //ordina posizioni
                f.fattura.Posizioni = f.fattura!.Posizioni?.OrderBy(p => p.NumeroLinea).ToList();
            }
        }
        var fattData = new FattureListaDto(); 

        fattData.AddRange(computedFatture);
        return fattData;
    }
}