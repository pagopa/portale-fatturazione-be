-- be.vwGestioneFattureFormAnniMesi: combinazioni tipologia/anno/mese/azione ammesse (pura date-math,
-- nessuna dipendenza da tabelle). Usata da modifica/anni, modifica/mesi, verifica azione.
-- Nota: colonna tipologia_fattura in Title Case ('Secondo Saldo'); il confronto con i valori uppercase
-- del command funziona perche' la collation SQL Server e' case-insensitive.
CREATE OR ALTER VIEW [be].[vwGestioneFattureFormAnniMesi] AS
WITH
mesi_anticipo AS (
    SELECT DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1) AS d
    UNION ALL SELECT DATEADD(MONTH, 1, d) FROM mesi_anticipo WHERE d < DATEFROMPARTS(YEAR(GETDATE()) + 1, 12, 1)
),
mesi_acconto AS (
    SELECT DATEADD(MONTH, -1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) AS d
    UNION ALL SELECT DATEADD(MONTH, 1, d) FROM mesi_acconto WHERE d < DATEFROMPARTS(YEAR(GETDATE()) + 1, 12, 1)
),
mesi_primo_saldo AS (
    SELECT DATEADD(MONTH, -2, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) AS d
    UNION ALL SELECT DATEADD(MONTH, 1, d) FROM mesi_primo_saldo WHERE d < DATEFROMPARTS(YEAR(GETDATE()) + 1, 12, 1)
),
mesi_secondo_saldo AS (
    SELECT DATEADD(MONTH, -3, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) AS d
    UNION ALL SELECT DATEADD(MONTH, 1, d) FROM mesi_secondo_saldo WHERE d < DATEFROMPARTS(YEAR(GETDATE()) + 1, 12, 1)
),
mesi_var_semestrale AS (
    SELECT DATEADD(MONTH, -1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) AS d
    UNION ALL SELECT DATEADD(MONTH, 1, d) FROM mesi_var_semestrale WHERE d < DATEFROMPARTS(YEAR(GETDATE()) + 1, 12, 1)
),
mesi_sem_sospesi AS (
    SELECT DATEADD(MONTH, -1, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) AS d
    UNION ALL SELECT DATEADD(MONTH, 1, d) FROM mesi_sem_sospesi WHERE d < DATEFROMPARTS(YEAR(GETDATE()) + 1, 12, 1)
)
SELECT CAST('Anticipo' AS varchar(30)) AS tipologia_fattura, YEAR(d) AS anno, MONTH(d) AS mese, CAST('ELIMINA' AS varchar(20)) AS Azione FROM mesi_anticipo
UNION ALL
SELECT CAST('Acconto' AS varchar(30)), YEAR(d), MONTH(d), CAST('ELIMINA' AS varchar(20)) FROM mesi_acconto
UNION ALL
SELECT CAST('Primo Saldo' AS varchar(30)), YEAR(d), MONTH(d), CAST('POSTICIPA' AS varchar(20)) FROM mesi_primo_saldo
UNION ALL
SELECT CAST('Secondo Saldo' AS varchar(30)), YEAR(d), MONTH(d), CAST('POSTICIPA' AS varchar(20)) FROM mesi_secondo_saldo
UNION ALL
SELECT CAST('Var. Semestrale' AS varchar(30)), YEAR(d), MONTH(d), CAST('POSTICIPA' AS varchar(20)) FROM mesi_var_semestrale
UNION ALL
SELECT CAST('Sem. Sospesi' AS varchar(30)), YEAR(d), MONTH(d), CAST('POSTICIPA' AS varchar(20)) FROM mesi_sem_sospesi;
