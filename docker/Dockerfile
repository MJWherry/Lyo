# Multi-stage, target-driven image for running Lyo benchmarks and tests in full isolation.
#
# The source is baked in and compiled in the build stage; the final stage copies only the built
# artifacts (plus the NuGet cache). The host source tree is NEVER mounted for building, so container
# builds can never touch the host's obj/bin. At runtime only docs/benchmarks/data is mounted out.
#
# A single TARGET arg selects which projects this image compiles and runs (see
# scripts/docker/resolve-targets.sh for the grammar: groups, project names, or a list). The Tesseract
# OCR native libs are installed automatically only when a selected project needs them.
#
# Both stages use the SDK image: BenchmarkDotNet's default toolchain compiles a per-run executable,
# so the SDK is required at runtime too.

# ---- build stage: compile only the selected projects -----------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG TARGET=all

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

WORKDIR /src
COPY . .

# Resolve the target to a csproj set, install the Tesseract native libs only if the OCR project is in
# it (its post-build target symlinks libleptonica/libtesseract into the output at build time), then
# build each selected project. Tools apps and the whole solution are never built.
RUN set -e; \
    projects="$(bash scripts/docker/resolve-targets.sh $TARGET)"; \
    echo "==> target '$TARGET' resolves to:"; echo "$projects" | sed 's/^/    /'; \
    if echo "$projects" | grep -q 'Lyo.Images.Ocr.Tesseract'; then \
      echo "==> installing Tesseract native libs (OCR project selected)"; \
      apt-get update \
      && apt-get install -y --no-install-recommends libleptonica-dev libtesseract5 tesseract-ocr-eng \
      && rm -rf /var/lib/apt/lists/*; \
    fi; \
    for p in $projects; do echo "==> building $p"; dotnet build -c Release "$p"; done

# ---- final stage: copy the built artifacts ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG TARGET=all

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

COPY docker/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

# Copy the warmed NuGet cache (BDN's per-run restore and `dotnet test --no-build` resolve offline)
# and the compiled source tree (source + bin/obj artifacts) from the build stage.
COPY --from=build /root/.nuget/packages /root/.nuget/packages
COPY --from=build /src /src

WORKDIR /src

# python3 runs scripts/benchmarks/build-manifests.py (stdlib only) after a bench run. The OCR tests
# also load the native libs at runtime (the build-stage symlinks in /src point back at these system
# libs), so install them here too when this image's target selects the OCR project.
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 \
    && if bash scripts/docker/resolve-targets.sh $TARGET | grep -q 'Lyo.Images.Ocr.Tesseract'; then \
         apt-get install -y --no-install-recommends libleptonica-dev libtesseract5 tesseract-ocr-eng; \
       fi \
    && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
