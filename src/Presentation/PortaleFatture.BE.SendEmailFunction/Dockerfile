# Immagine della Azure Function SendEmail (fat-{d,u,p}-api-func).
#
# ATTENZIONE: il build context e' la ROOT del repository, non questa cartella
# (i COPY sotto usano percorsi relativi src/...):
#   docker build -f src/Presentation/PortaleFatture.BE.SendEmailFunction/Dockerfile .

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0@sha256:027ed0abff13b7f0b0595f0865db8c50efea22a34906801daf0ca521e66a4bd5 AS base

# A differenza del Dockerfile della Function.API, qui NON si installano le librerie native
# di wkhtmltopdf (libjpeg62, libxrender1, libfontconfig1, libfreetype6, libx11-6, xfonts-*):
# questo progetto usa solo i metodi HTML di DocumentBuilder/DocumentPspBuilder
# (CreateEmailHtml, CreateEmailAdjsutmentHtml) e non chiama mai CreatePdf.
# Se un domani una function di questo progetto generasse un PDF, va ripreso il blocco
# apt-get della Function.API: senza quelle librerie si ottiene una DllNotFoundException
# a runtime, non un errore di build.

WORKDIR /home/site/wwwroot
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:d80fdd84f7e18eea12f8e45c52914f1353395009c95c41197178ea19944e6d48 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Chiusura transitiva delle ProjectReference: la function referenzia Infrastructure e Api,
# che a loro volta portano Core.
# NB: il csproj di questo progetto usa i trattini bassi, mentre la cartella usa i punti.
COPY ["src/Presentation/PortaleFatture.BE.SendEmailFunction/PortaleFatture_BE_SendEmailFunction.csproj", "src/Presentation/PortaleFatture.BE.SendEmailFunction/"]
COPY ["src/Infrastructure/PortaleFatture.BE.Infrastructure/PortaleFatture.BE.Infrastructure.csproj", "src/Infrastructure/PortaleFatture.BE.Infrastructure/"]
COPY ["src/Core/PortaleFatture.BE.Core/PortaleFatture.BE.Core.csproj", "src/Core/PortaleFatture.BE.Core/"]
COPY ["src/Presentation/PortaleFatture.BE.Api/PortaleFatture.BE.Api.csproj", "src/Presentation/PortaleFatture.BE.Api/"]
RUN dotnet restore "./src/Presentation/PortaleFatture.BE.SendEmailFunction/PortaleFatture_BE_SendEmailFunction.csproj"
COPY . .
WORKDIR "/src/src/Presentation/PortaleFatture.BE.SendEmailFunction"
RUN dotnet build "./PortaleFatture_BE_SendEmailFunction.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release

RUN dotnet publish "./PortaleFatture_BE_SendEmailFunction.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
