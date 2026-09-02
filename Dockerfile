# syntax=docker/dockerfile:1
#
# Shared, parameterized Dockerfile for every EventPlatform .NET service and the
# gateway — one Dockerfile, not seven near-identical copies, so dev and a future
# prod build identically. Build context is the repo root (project references
# reach into building-blocks/ and the root Directory.Build.props/Directory.Packages.props
# — see .dockerignore for what's excluded from that context).
#
# Build with:
#   docker build -f Dockerfile \
#     --build-arg PROJECT_PATH=services/catalog/Catalog.Api/Catalog.Api.csproj \
#     --build-arg ASSEMBLY_NAME=Catalog.Api \
#     -t <tag> .
#
# PROJECT_PATH/ASSEMBLY_NAME per target (ASSEMBLY_NAME is the .csproj name minus
# the extension, i.e. the published .dll name):
#   services/catalog/Catalog.Api/Catalog.Api.csproj             -> Catalog.Api
#   services/inventory/Inventory.Api/Inventory.Api.csproj       -> Inventory.Api
#   services/ordering/Ordering.Api/Ordering.Api.csproj          -> Ordering.Api
#   services/payments/Payments.Api/Payments.Api.csproj          -> Payments.Api
#   services/ticketing/Ticketing.Api/Ticketing.Api.csproj       -> Ticketing.Api
#   services/communication/Communication.Api/Communication.Api.csproj -> Communication.Api
#   services/media/Media.Api/Media.Api.csproj                   -> Media.Api
#   services/identity/Identity.Api/Identity.Api.csproj           -> Identity.Api
#   services/queue/Queue.Api/Queue.Api.csproj                    -> Queue.Api
#   services/venue/Venues.Api/Venues.Api.csproj                  -> Venues.Api
#   gateways/EventPlatform.Gateway/EventPlatform.Gateway.csproj -> EventPlatform.Gateway

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT_PATH
WORKDIR /src
COPY . .
RUN dotnet restore "${PROJECT_PATH}"
RUN dotnet publish "${PROJECT_PATH}" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG ASSEMBLY_NAME
ENV ASSEMBLY_NAME=${ASSEMBLY_NAME} \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
WORKDIR /app
COPY --from=build /app/publish .

# Every mcr.microsoft.com/dotnet/aspnet image since .NET 8 ships a non-root
# "app" user (UID 64198) — run as that, not root.
USER app

EXPOSE 8080

# `sh -c` is needed to expand ${ASSEMBLY_NAME}, but a bare `sh -c '<script>'` assigns any further
# arguments starting at $0 — so a Kubernetes `args: ["--migrate"]` would be silently swallowed and
# the container would serve traffic instead of applying migrations. The trailing "--" fills the $0
# slot so real arguments land in "$@" and reach the app. Keep both parts if you touch this line.
ENTRYPOINT ["sh", "-c", "exec dotnet \"${ASSEMBLY_NAME}.dll\" \"$@\"", "--"]
