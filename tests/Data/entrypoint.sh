#!/bin/bash
set -e

if [ "$1" = '/opt/mssql/bin/sqlservr' ]; then
  # If this is the container's first run, initialize the application database
  if [ ! -f /tmp/app-initialized ]; then
    # Initialize the application database asynchronously in a background process. This allows a) the SQL Server process to be the main process in the container, which allows graceful shutdown and other goodies, and b) us to only start the SQL Server process once, as opposed to starting, stopping, then starting it again.
    function initialize_app_database() {
      # Wait a bit for SQL Server to start. SQL Server's process doesn't provide a clever way to check if it's up or not, and it needs to be up before we can import the application database
      sleep 15s

      # tools path: su immagine 2025 e' mssql-tools18 (con -C per fidarsi del cert self-signed)
      SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
      [ -x "$SQLCMD" ] || SQLCMD="/opt/mssql-tools/bin/sqlcmd"

      #run the setup script to create the DB and the schema in the DB
      "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i /scripts/setup.sql

      # schema/tabelle/seed per i test CRUD di Gestione Fatture (PF-672)
      "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i /scripts/gestione_fatture.sql

      # viste be.vwGestioneFatture* (tests/Data/views/), dopo tabelle/seed
      if [ -d /scripts/views ]; then
        for f in $(ls /scripts/views/*.sql 2>/dev/null | sort); do
          echo "Applying view script: $f"
          "$SQLCMD" -C -S localhost -U sa -P 52JdGnzZaANhf -d master -i "$f"
        done
      fi

      # stored procedure autorevoli be.spGestioneFattura* (droppate in tests/Data/sp/), in ordine
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