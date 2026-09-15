# Multi-stage Dockerfile for API Gateway Service
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Repository-wide build settings and central package versions must be present before restore
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", ".editorconfig", "./"]

# Copy shared projects first for better layer caching
COPY ["Shared/Store.Shared/Store.Shared.csproj", "Shared/Store.Shared/"]

# Copy service project
COPY ["Gateway/APIGateway/Store.GatewayService.csproj", "Gateway/APIGateway/"]

# Restore dependencies
RUN dotnet restore "Gateway/APIGateway/Store.GatewayService.csproj"

# Copy source code
COPY ["Gateway/APIGateway/", "Gateway/APIGateway/"]
COPY ["Shared/", "Shared/"]

# Build the application
WORKDIR "/src/Gateway/APIGateway"
RUN dotnet build "Store.GatewayService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Store.GatewayService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "Store.GatewayService.dll"]
