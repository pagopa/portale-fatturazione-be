#!/bin/bash
set -e

if [ "$1" = '/opt/mssql/bin/sqlservr' ]; then
  # If this is the container's first run, initialize the application database
  if [ ! -f /tmp/app-initialized ]; then
    # Initialize the application database asynchronously in a background process. This allows a) the SQL Server process to be the main process in the container, which allows graceful shutdown and other goodies, and b) us to only start the SQL Server process once, as opposed to starting, stopping, then starting it again.
    function initialize_app_database() {
      # tools path: su immagine 2025 e' mssql-tools18 (con -C per fidarsi del cert self-signed)
      SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
      [ -x "$SQLCMD" ] || SQLCMD="/opt/mssql-tools/bin/sqlcmd"

      # Attesa basata sulla DISPONIBILITA' REALE del server, non su uno sleep fisso: con un semplice
      # "sleep 15" su macchine lente il login fallisce ("An error occurred while evaluating the
      # password") e tutti gli script di init saltano, lasciando il DB vuoto.
      echo "Waiting for SQL Server to accept connections..."
      for i in $(seq 1 90); do
        if "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -Q "SELECT 1" >/dev/null 2>&1; then
          echo "SQL Server ready after ${i} attempt(s)."
          break
        fi
        sleep 2
      done

      #run the setup script to create the DB and the schema in the DB
      "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i /scripts/setup.sql

      # schema/tabelle/seed per i test CRUD di Gestione Fatture (PF-672)
      "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i /scripts/gestione_fatture.sql

      # ente/contratto con codiceSDI per i test su DatiFatturazione (PF-705)
      "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i /scripts/dati_fatturazione.sql

      # notifiche + contestazioni + calendario contestazioni (seed 2026/3 aperto, 2026/4 chiuso),
      # per POST api/notifiche/pagopa e per la matrice azioni di AzioneContestazione*
      "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i /scripts/notifiche.sql

      # modulo commessa: tabelle mancanti + seed 2026/4-5 per le rotte api/v2/modulocommessa/*
      # (deve precedere views/, che contiene le 4 viste legacy pfd.v* dell'area)
      "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i /scripts/modulo_commessa.sql

      # orchestratore: cfg.CalendarioVarSemestrale + cfg.CalendarioFatturazione (temporale) e seed.
      # Deve precedere views/, che contiene pfd.vOrchestratore: un CREATE VIEW fallisce se le tabelle
      # referenziate non esistono (per le viste non c'e' deferred name resolution).
      "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i /scripts/orchestratore.sql

      # chiavi API e whitelist IP delle Integration API (con i due indici univoci, che sono contratto)
      "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i /scripts/api_keys.sql

      # anteprima email PSP (stg.PspEmailPreview + colonne nullable su ppa.PspEmail): erano script
      # da lanciare a mano, quindi un rebuild del container li perdeva. Sono idempotenti.
      "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i /scripts/create_psp_email_preview_table.sql
      "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i /scripts/alter_ppa_pspemail_add_nullable_columns.sql

      # viste be.vwGestioneFatture* (tests/Data/views/), dopo tabelle/seed
      if [ -d /scripts/views ]; then
        for f in $(ls /scripts/views/*.sql 2>/dev/null | sort); do
          echo "Applying view script: $f"
          "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i "$f"
        done
      fi

      # stored procedure be.spGestioneFattura* (droppate in tests/Data/sp/), in ordine
      if [ -d /scripts/sp ]; then
        for f in $(ls /scripts/sp/*.sql 2>/dev/null | sort); do
          echo "Applying SP script: $f"
          "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i "$f"
        done
      fi

      # Note that the container has been initialized so future starts won't wipe changes to the data
      touch /tmp/app-initialized
    }
    initialize_app_database &
  fi
fi

exec "$@"