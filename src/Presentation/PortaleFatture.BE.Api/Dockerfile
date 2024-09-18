#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:84a93198d134a82a8f41c88b96adc6bfc2caf1d91ad25d5f25d90279938e1c4d AS base
USER app
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:a364676fedc145cf88caad4bfb3cc372aae41e596c54e8a63900a2a1c8e364c6 AS build

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Presentation/PortaleFatture.BE.Api/PortaleFatture.BE.Api.csproj", "src/Presentation/PortaleFatture.BE.Api/"]
COPY ["src/Infrastructure/PortaleFatture.BE.Infrastructure/PortaleFatture.BE.Infrastructure.csproj", "src/Infrastructure/PortaleFatture.BE.Infrastructure/"]
COPY ["src/Core/PortaleFatture.BE.Core/PortaleFatture.BE.Core.csproj", "src/Core/PortaleFatture.BE.Core/"]
RUN dotnet restore "./src/Presentation/PortaleFatture.BE.Api/./PortaleFatture.BE.Api.csproj"
COPY . .
WORKDIR "/src/src/Presentation/PortaleFatture.BE.Api"
RUN dotnet build "./PortaleFatture.BE.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PortaleFatture.BE.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PortaleFatture.BE.Api.dll"]