# Parameterized image for the local workbench stack (TestApi, TestGateway, job examples).
#
# Build context is the repo root so Directory.Packages.props and ProjectReferences resolve.
#   docker build -f docker/workbench/Dockerfile --build-arg PROJECT=Lyo.Net/Tools/Lyo.TestApi/Lyo.TestApi.csproj .
#
# INSTALL_CHROME=1 installs Google Chrome plus a --no-sandbox wrapper for Puppeteer (TestGateway HTML→PDF).
# Ubuntu's `chromium` apt package is a snap stub and does not work in Docker.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG PROJECT=Lyo.Net/Tools/Lyo.TestApi/Lyo.TestApi.csproj

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

WORKDIR /src
COPY . .

RUN dotnet publish "$PROJECT" -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0

ARG INSTALL_CHROME=0

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DEBIAN_FRONTEND=noninteractive \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_ENVIRONMENT=Development

WORKDIR /app

# curl: compose healthcheck on TestApi /health. fonts: ClosedXML / SixLabors fallback (same as the runner image).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl fontconfig fonts-dejavu-core \
    && if [ "$INSTALL_CHROME" = "1" ]; then \
         curl -fsSL -o /tmp/google-chrome.deb https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb \
         && apt-get install -y --no-install-recommends /tmp/google-chrome.deb \
         && rm -f /tmp/google-chrome.deb \
         && printf '%s\n' \
              '#!/bin/sh' \
              'exec /usr/bin/google-chrome --no-sandbox --disable-dev-shm-usage --disable-gpu "$@"' \
              > /usr/local/bin/chromium-docker \
         && chmod +x /usr/local/bin/chromium-docker; \
       fi \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

EXPOSE 8080

ENTRYPOINT ["dotnet"]
CMD ["Lyo.TestApi.dll"]
