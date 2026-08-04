# Lyo.Portfolio.Api — multi-stage publish for the portfolio API EC2.
# Build from repo root:
#   docker build -f deploy/portfolio/api/Dockerfile -t lyo-portfolio-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

COPY Lyo.Net/ ./Lyo.Net/

RUN dotnet restore Lyo.Net/Apps/Portfolio/Lyo.Portfolio.Api/Lyo.Portfolio.Api.csproj \
 && dotnet publish Lyo.Net/Apps/Portfolio/Lyo.Portfolio.Api/Lyo.Portfolio.Api.csproj \
      -c Release \
      -o /app/publish \
      --no-restore \
      /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /var/lyo/portfolio-files

ENV ASPNETCORE_URLS=http://+:5251 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

EXPOSE 5251
ENTRYPOINT ["dotnet", "Lyo.Portfolio.Api.dll"]
