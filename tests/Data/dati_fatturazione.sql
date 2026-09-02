-- Seed dedicato ai test su DatiFatturazione (PF-705).
--
-- L'handler DatiFatturazioneCreateCommandHandler, prima di scrivere, legge il contratto dell'ente
-- con EnteCodiceSDIQueryGetByIdPersistence (JOIN pfd.Enti + pfd.Contratti + pfw.TipoContratto):
-- se l'ente non esiste il risultato e' null e l'handler va in NullReferenceException su
-- contratto.CodiceSDI. Serve quindi un ente REALE nel seed, non un Guid casuale.
--
-- Usiamo un ente dedicato per non interferire con quelli di Gestione Fatture (PF-672).

-- Un ente per fixture: i test creano/aggiornano la riga DatiFatturazione dello stesso ente e la
-- ripuliscono in SetUp/TearDown, quindi due fixture che condividessero l'ente si disturberebbero.
--   4444 -> DatiFatturazioneCreateCommandTests
--   5555 -> DatiFatturazioneGetByIdQueryTests
--   6666 -> DatiFatturazioneUpdateCommandTests
IF NOT EXISTS (SELECT 1 FROM pfd.Enti WHERE InternalIstitutionId = '44444444-4444-4444-4444-444444444444')
INSERT INTO pfd.Enti (InternalIstitutionId, description, institutionType, originId) VALUES
 ('44444444-4444-4444-4444-444444444444', 'Ente Dati Fatturazione', 'PA', 'c_test01'),
 ('55555555-5555-5555-5555-555555555555', 'Ente Dati Fatturazione GetById', 'PA', 'c_test02'),
 ('66666666-6666-6666-6666-666666666666', 'Ente Dati Fatturazione Update', 'PA', 'c_test03');
GO

-- codiceSDI valorizzato: l'handler confronta command.CodiceSDI con contratto.CodiceSDI per
-- decidere skipVerifica. Con lo stesso valore il test copre il ramo "SDI invariato".
IF NOT EXISTS (SELECT 1 FROM pfd.Contratti WHERE internalistitutionid = '44444444-4444-4444-4444-444444444444')
INSERT INTO pfd.Contratti (internalistitutionid, product, FkIdTipoContratto, codiceSDI, createdat) VALUES
 ('44444444-4444-4444-4444-444444444444', 'prod-pn', 2, 'ABCDEF1', GETUTCDATE()),
 ('55555555-5555-5555-5555-555555555555', 'prod-pn', 2, 'ABCDEF1', GETUTCDATE()),
 ('66666666-6666-6666-6666-666666666666', 'prod-pn', 2, 'ABCDEF1', GETUTCDATE());
GO

-- Gli enti di Gestione Fatture ricevono comunque un codiceSDI, cosi' la stessa query non esplode
-- se un test futuro li riusa.
UPDATE pfd.Contratti SET codiceSDI = 'SDITEST'
WHERE codiceSDI IS NULL
  AND internalistitutionid IN (
    '11111111-1111-1111-1111-111111111111',
    '22222222-2222-2222-2222-222222222222',
    '33333333-3333-3333-3333-333333333333',
    '53b40136-65f2-424b-acfb-7fae17e35c60');
GO
