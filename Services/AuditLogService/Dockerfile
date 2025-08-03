# Multi-stage Dockerfile for Audit Log Service
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5005
ENV ASPNETCORE_URLS=http://+:5005

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared projects first for better layer caching
COPY ["Shared/Store.Shared/Store.Shared.csproj", "Shared/Store.Shared/"]
COPY ["Shared/Store.Shared.DTOs/Store.Shared.DTOs.csproj", "Shared/Store.Shared.DTOs/"]
COPY ["Shared/Store.Shared.Contracts/Store.Shared.Contracts.csproj", "Shared/Store.Shared.Contracts/"]

# Copy service project
COPY ["Services/AuditLogService/Store.AuditLogService.csproj", "Services/AuditLogService/"]

# Restore dependencies
RUN dotnet restore "Services/AuditLogService/Store.AuditLogService.csproj"

# Copy source code
COPY ["Services/AuditLogService/", "Services/AuditLogService/"]
COPY ["Shared/", "Shared/"]

# Build the application
WORKDIR "/src/Services/AuditLogService"
RUN dotnet build "Store.AuditLogService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Store.AuditLogService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Store.AuditLogService.dll"]
