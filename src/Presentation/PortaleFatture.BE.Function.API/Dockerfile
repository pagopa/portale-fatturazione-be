FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0@sha256:ff3c36b7ca1bf3e0535f6783332525d2e03b8caf6ba24cb4247c54cebe980534 AS base

RUN apt-get update && apt-get install -y \
    libjpeg62 \
    libxrender1 \
    libxext6 \
    libfontconfig1 \
    libfreetype6 \
    libx11-6 \
    xfonts-base \
    xfonts-75dpi \
    && apt-get clean

#COPY wkhtmltox/lib/* /usr/local/lib/
#COPY wkhtmltox/bin/wkhtmltopdf /usr/local/bin/
#RUN chmod +x /usr/local/bin/wkhtmltopdf

ENV LD_LIBRARY_PATH=/usr/local/lib:$LD_LIBRARY_PATH
ENV PATH="/usr/local/bin:${PATH}"

WORKDIR /home/site/wwwroot
EXPOSE 8080 

FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:9e9690cacf04e7e37722dfd0c93ad99129df850db79c2947704be0752f3e62ac AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Presentation/PortaleFatture.BE.Function.API/PortaleFatture.BE.Function.API.csproj", "src/Presentation/PortaleFatture.BE.Function.API/"]
COPY ["src/Infrastructure/PortaleFatture.BE.Infrastructure/PortaleFatture.BE.Infrastructure.csproj", "src/Infrastructure/PortaleFatture.BE.Infrastructure/"]
COPY ["src/Core/PortaleFatture.BE.Core/PortaleFatture.BE.Core.csproj", "src/Core/PortaleFatture.BE.Core/"]
COPY ["src/Presentation/PortaleFatture.BE.Api/PortaleFatture.BE.Api.csproj", "src/Presentation/PortaleFatture.BE.Api/"]
RUN dotnet restore "./src/Presentation/PortaleFatture.BE.Function.API/PortaleFatture.BE.Function.API.csproj"
COPY . .
WORKDIR "/src/src/Presentation/PortaleFatture.BE.Function.API"
RUN dotnet build "./PortaleFatture.BE.Function.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release 
 
RUN dotnet publish "./PortaleFatture.BE.Function.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish /p:UseAppHost=false 

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated