<#
.SYNOPSIS
    Esegue gli integration test sul DB seedato locale, occupandosi del container.

.DESCRIPTION
    Porta d'ingresso unica per i test che girano sul container SQL Server di tests/. Esiste per
    chiudere i due modi in cui questi test ingannano chi li lancia:

      1. CONTAINER SPENTO -> i test NON falliscono: TestDb.SkipIfUnavailable li degrada a "Ignorati"
         e `dotnet test` stampa comunque "Superato!". Si crede di aver verificato qualcosa e non si
         e' eseguito nulla. E' il caso peggiore, perche' e' silenzioso.

      2. CONTAINER AVVIATO MA SEED NON FINITO -> rossi fuorvianti del tipo
         "Invalid object name 'be.vwDettaglioFattureDaInviare'": l'inizializzazione gira in
         background, quindi SQL Server accetta connessioni prima che viste e stored procedure siano
         state applicate. Sembra un seed rotto e non lo e'.

    Lo script avvia il container se serve, ASPETTA che l'inizializzazione sia davvero completa
    (marker /tmp/app-initialized scritto dall'entrypoint) e solo allora lancia i test.

    NON modifica alcun file: ne' il Dockerfile della SendEmailFunction, ne' tests/Data/Dockerfile,
    ne' docker-compose.yml. Si limita a invocarli.

.PARAMETER Filter
    Filtro passato a `dotnet test --filter`. ATTENZIONE: filtrare su un frammento contenuto nel
    namespace (es. "Fatture", "BE", "Portale") seleziona l'INTERA suite, perche' il namespace e'
    PortaleFatture.BE.IntegrationTest. Filtrare sul nome della CLASSE.

.PARAMETER Rebuild
    Ricostruisce il container da zero (docker compose down -v && up --build). DISTRUTTIVO: cancella
    il volume e con esso i dati. Serve dopo aver modificato gli script di tests/Data/.

.PARAMETER Function
    Alza anche il profilo "function": la SendEmailFunction containerizzata + Azurite, su cui girano i
    test end-to-end che passano dal webhook HTTP e dal polling dell'orchestrazione
    (CreateRelRigheFunctionHostIntegrationTests). Senza questo switch quei test si auto-ignorano.
    ATTENZIONE: la prima volta costruisce un'immagine da ~2,5 GB, quindi non e' un giro veloce.

.PARAMETER Stop
    Ferma il container alla fine (senza -v: i dati restano).

.PARAMETER TimeoutSeconds
    Attesa massima per il completamento del seed. Default 300.

.EXAMPLE
    .\run-integration.ps1 -Filter "FullyQualifiedName~EmailRelService"

.EXAMPLE
    .\run-integration.ps1 -Rebuild      # dopo aver toccato tests/Data/*.sql
#>
[CmdletBinding()]
param(
    [string]$Filter,
    [switch]$Rebuild,
    [switch]$Function,
    [switch]$Stop,
    [int]$TimeoutSeconds = 300
)

$ErrorActionPreference = "Stop"
$tests = $PSScriptRoot
$container = "portalefatture_db"
$progetto = Join-Path $tests "PortaleFatture.BE.IntegrationTest\PortaleFatture.BE.IntegrationTest.csproj"

function Fallisci($messaggio) {
    Write-Host "[run-integration] $messaggio" -ForegroundColor Red
    exit 1
}

# --- 1. Docker disponibile? ------------------------------------------------------------------
try { docker info 2>&1 | Out-Null } catch { }
if ($LASTEXITCODE -ne 0) {
    Fallisci "Docker non risponde. Avvia Docker Desktop e rilancia."
}

Push-Location $tests
try {
    # --- 2. Container su ---------------------------------------------------------------------
    if ($Rebuild) {
        Write-Host "[run-integration] Ricostruzione da zero (il volume viene cancellato)..." -ForegroundColor Yellow
        docker compose down -v | Out-Null
        docker compose up -d --build | Out-Null
    }
    else {
        $attivo = (docker ps --filter "name=$container" --format "{{.Names}}") -eq $container
        if (-not $attivo) {
            Write-Host "[run-integration] Container non attivo, lo avvio..." -ForegroundColor Yellow
            docker compose up -d | Out-Null
        }
    }
    if ($LASTEXITCODE -ne 0) { Fallisci "Avvio del container fallito." }

    # --- 3. Attesa: servono DUE condizioni, non una ------------------------------------------
    # /tmp/app-initialized dice "il seed e' stato applicato UNA VOLTA", non "il server e' pronto
    # adesso": il file vive nel filesystem del container, quindi sopravvive a stop/start. Dopo un
    # riavvio e' gia' li' mentre SQL Server sta ancora salendo — misurato il 15/09/2026: lo script
    # ripartiva subito e tutti i test fallivano. Serve quindi anche una query di prova.
    #
    # La password SA viene letta dal docker-compose.yml invece di essere riscritta qui: e' gia' in
    # chiaro in quel file e nell'entrypoint (credenziale di un container usa-e-getta locale), e
    # duplicarla una terza volta renderebbe solo piu' difficile toglierla di mezzo un domani.
    $compose = Join-Path $tests "docker-compose.yml"
    $sa = (Select-String -Path $compose -Pattern 'SA_PASSWORD:\s*"?([^"\s]+)"?').Matches[0].Groups[1].Value
    if (-not $sa) { Fallisci "Password SA non trovata in docker-compose.yml." }

    Write-Host "[run-integration] Attendo che il DB sia pronto e il seed applicato..." -NoNewline
    $scaduto = (Get-Date).AddSeconds($TimeoutSeconds)
    while ($true) {
        $seedApplicato = $false
        docker exec $container test -f /tmp/app-initialized 2>$null
        if ($LASTEXITCODE -eq 0) { $seedApplicato = $true }

        $rispondeDavvero = $false
        if ($seedApplicato) {
            # Una query su un oggetto del seed, non "SELECT 1": copre sia il server che sale sia il
            # caso di un seed a meta'.
            docker exec $container /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P $sa `
                -d master -Q "SET NOCOUNT ON; SELECT TOP 1 1 FROM pfd.Enti;" 2>$null | Out-Null
            if ($LASTEXITCODE -eq 0) { $rispondeDavvero = $true }
        }

        if ($seedApplicato -and $rispondeDavvero) { Write-Host " pronto." -ForegroundColor Green; break }

        if ((Get-Date) -gt $scaduto) {
            Write-Host ""
            Write-Host "[run-integration] Ultime righe di log del container:" -ForegroundColor Yellow
            docker logs --tail 20 $container
            Fallisci "DB non pronto entro $TimeoutSeconds s. I test non sono stati eseguiti."
        }
        Write-Host "." -NoNewline
        Start-Sleep -Seconds 3
    }

    # --- 3-bis. Profilo "function" ------------------------------------------------------------
    # La function ha bisogno del DB gia' pronto (legge CONNECTION_STRING all'esecuzione) e di
    # Azurite, senza il quale i listener Durable non partono affatto.
    if ($Function) {
        Write-Host "[run-integration] Avvio Azurite e la SendEmailFunction containerizzata..." -ForegroundColor Yellow
        # SEMPRE --build: l'immagine deve seguire il sorgente, altrimenti i test end-to-end
        # verificherebbero il binario del giro precedente e sarebbero un falso verde. La cache dei
        # layer rende il rebuild incrementale, e qui NON si tocca il volume del DB (quello lo
        # cancella solo -Rebuild, che agisce sul profilo di default).
        docker compose --profile function up -d --build | Out-Null
        if ($LASTEXITCODE -ne 0) { Fallisci "Avvio del profilo 'function' fallito." }

        # La sonda e' l'handler chiamato senza parametri: risponde 400 per costruzione, quindi dice
        # in un colpo solo che l'host e' su E che la function e' stata indicizzata (un problema di
        # indicizzazione darebbe 404). Un "connection refused" significa che sta ancora salendo.
        $urlSonda = "http://localhost:8080/api/CreateRelRigheHandler"
        Write-Host "[run-integration] Attendo la function su $urlSonda..." -NoNewline
        $scadutoFn = (Get-Date).AddSeconds($TimeoutSeconds)
        while ($true) {
            $codice = 0
            try { $codice = (Invoke-WebRequest -Uri $urlSonda -TimeoutSec 5 -SkipHttpErrorCheck).StatusCode } catch { }

            if ($codice -eq 400) { Write-Host " pronta." -ForegroundColor Green; break }
            if ($codice -eq 404) {
                Write-Host ""
                docker logs --tail 30 portalefatture_sendemail
                Fallisci "L'host risponde ma la function non e' indicizzata (404). I test non sono stati eseguiti."
            }
            if ((Get-Date) -gt $scadutoFn) {
                Write-Host ""
                docker logs --tail 30 portalefatture_sendemail
                Fallisci "Function host non pronto entro $TimeoutSeconds s. I test non sono stati eseguiti."
            }
            Write-Host "." -NoNewline
            Start-Sleep -Seconds 3
        }
    }

    # --- 4. Test ------------------------------------------------------------------------------
    $argomenti = @("test", $progetto, "-nodeReuse:false")
    if ($Filter) { $argomenti += @("--filter", $Filter) }

    Write-Host "[run-integration] dotnet $($argomenti -join ' ')" -ForegroundColor Cyan
    & dotnet @argomenti
    $esito = $LASTEXITCODE

    # Un "Superato!" con tutti i test ignorati non e' un successo: qui il container c'e' di sicuro,
    # quindi un ignorato residuo riguarda i test che puntano a UAT (VPN) o quelli marcati [Ignore].
    if ($esito -eq 0) {
        Write-Host "[run-integration] Test completati." -ForegroundColor Green
    }
    else {
        Write-Host "[run-integration] Test falliti (exit $esito)." -ForegroundColor Red
    }

    if ($Stop) {
        Write-Host "[run-integration] Fermo i container (i dati restano)." -ForegroundColor Yellow
        # --profile function e' necessario anche per FERMARLI: senza, compose non li considera.
        docker compose --profile function stop | Out-Null
    }

    exit $esito
}
finally {
    Pop-Location
}
