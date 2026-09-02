# Stored procedure Gestione Fatture (PF-672)

Metti qui i 4 script SP **autorevoli** (quelli reali del DB), uno per file:

- `10_spGestioneFatturePosticipa.sql`
- `20_spGestioneFattureElimina.sql`
- `30_spGestioneFattureRipristina.sql`
- `40_spGestioneFattureCancella.sql`

Devono creare le procedure nello schema `be` (es. `CREATE PROCEDURE [be].[spGestioneFatturePosticipa] ...`).
L'entrypoint li esegue in ordine alfabetico DOPO `gestione_fatture.sql` (che crea schema/tabelle/seed).
Il prefisso numerico serve solo a fissare l'ordine.
