# Multi-stage Dockerfile for Cart Service
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
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
COPY ["Shared/Store.Contracts/Store.Contracts.csproj", "Shared/Store.Contracts/"]
COPY ["Shared/Store.BuildingBlocks/Store.BuildingBlocks.csproj", "Shared/Store.BuildingBlocks/"]

# Copy service project
COPY ["Services/CartService/Store.CartService.csproj", "Services/CartService/"]

# Restore dependencies
RUN dotnet restore "Services/CartService/Store.CartService.csproj"

# Copy source code
COPY ["Services/CartService/", "Services/CartService/"]
COPY ["Shared/", "Shared/"]

# Build the application
WORKDIR "/src/Services/CartService"
RUN dotnet build "Store.CartService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Store.CartService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


ENTRYPOINT ["dotnet", "Store.CartService.dll"]
