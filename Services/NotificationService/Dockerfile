# syntax=docker/dockerfile:1.7
# Notification service (the e-mails that follow orders, payments, shipping and stock). Built from the repository root: docker build -f Services/NotificationService/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore from the project files alone, so this layer survives source changes
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY Shared/Store.Contracts/Store.Contracts.csproj Shared/Store.Contracts/
COPY Shared/Store.BuildingBlocks/Store.BuildingBlocks.csproj Shared/Store.BuildingBlocks/
COPY Services/NotificationService/Store.NotificationService.csproj Services/NotificationService/
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet restore Services/NotificationService/Store.NotificationService.csproj

COPY Shared/ Shared/
COPY Services/NotificationService/ Services/NotificationService/
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet publish Services/NotificationService/Store.NotificationService.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

# Chiseled runtime: no shell, no package manager, runs as the unprivileged "app" user. Health is
# probed over HTTP by whatever runs the container (Container Apps probes, the compose wait loop).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Store.NotificationService.dll"]
