# Lyo.Comic.Api — multi-stage publish.
# Build from repo root:
#   docker build -f deploy/comic/api/Dockerfile -t lyo-comic-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

COPY Lyo.Net/ ./Lyo.Net/

RUN dotnet restore Lyo.Net/Apps/Comic/Lyo.Comic.Api/Lyo.Comic.Api.csproj \
 && dotnet publish Lyo.Net/Apps/Comic/Lyo.Comic.Api/Lyo.Comic.Api.csproj \
      -c Release \
      -o /app/publish \
      --no-restore \
      /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /var/lyo/comic-files

ENV ASPNETCORE_URLS=http://+:5000 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

EXPOSE 5000
ENTRYPOINT ["dotnet", "Lyo.Comic.Api.dll"]
