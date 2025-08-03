# Multi-stage Dockerfile for Identity Service
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared projects first for better layer caching
COPY ["Shared/Store.Shared/Store.Shared.csproj", "Shared/Store.Shared/"]
COPY ["Shared/Store.Shared.DTOs/Store.Shared.DTOs.csproj", "Shared/Store.Shared.DTOs/"]
COPY ["Shared/Store.Shared.Contracts/Store.Shared.Contracts.csproj", "Shared/Store.Shared.Contracts/"]

# Copy service project
COPY ["Services/IdentityService/Store.IdentityService.csproj", "Services/IdentityService/"]

# Restore dependencies
RUN dotnet restore "Services/IdentityService/Store.IdentityService.csproj"

# Copy source code
COPY ["Services/IdentityService/", "Services/IdentityService/"]
COPY ["Shared/", "Shared/"]

# Build the application
WORKDIR "/src/Services/IdentityService"
RUN dotnet build "Store.IdentityService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Store.IdentityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Store.IdentityService.dll"]
