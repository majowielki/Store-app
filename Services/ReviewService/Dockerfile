# syntax=docker/dockerfile:1.7
# Review service (product reviews, their moderation and rating summaries). Built from the repository root: docker build -f Services/ReviewService/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Restore from the project files alone, so this layer survives source changes
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY Shared/Store.Contracts/Store.Contracts.csproj Shared/Store.Contracts/
COPY Shared/Store.BuildingBlocks/Store.BuildingBlocks.csproj Shared/Store.BuildingBlocks/
COPY Services/ReviewService/Store.ReviewService.csproj Services/ReviewService/
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet restore Services/ReviewService/Store.ReviewService.csproj

COPY Shared/ Shared/
COPY Services/ReviewService/ Services/ReviewService/
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet publish Services/ReviewService/Store.ReviewService.csproj -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

# Chiseled runtime: no shell, no package manager, runs as the unprivileged "app" user. Health is
# probed over HTTP by whatever runs the container (Container Apps probes, the compose wait loop).
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Store.ReviewService.dll"]
