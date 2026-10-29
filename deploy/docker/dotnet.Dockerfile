# syntax=docker/dockerfile:1.7-labs
# One image recipe for every .NET host. Build with:
#   docker build -f deploy/docker/dotnet.Dockerfile --build-arg PROJECT=src/Services/Orders/LogistiFlow.Orders.Api \
#                --build-arg ASSEMBLY=LogistiFlow.Orders.Api.dll .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROJECT
WORKDIR /source

# Restore in its own layer: it only re-runs when a project file or the central package versions change.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY --parents src/**/*.csproj ./
RUN dotnet restore "${PROJECT}"

COPY src/ src/
RUN dotnet publish "${PROJECT}" -c Release -o /app --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG ASSEMBLY
ENV APP_ASSEMBLY=${ASSEMBLY} \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_gcServer=1
WORKDIR /app
COPY --from=build /app .
USER ${APP_UID}
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=6 CMD ["sh", "-c", "dotnet \"$APP_ASSEMBLY\" --health-probe"]
ENTRYPOINT ["sh", "-c", "exec dotnet \"$APP_ASSEMBLY\" \"$@\"", "--"]
