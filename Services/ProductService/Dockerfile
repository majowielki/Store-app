# Multi-stage Dockerfile for Product Service
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5002

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared projects first for better layer caching
COPY ["Shared/Store.Shared/Store.Shared.csproj", "Shared/Store.Shared/"]
COPY ["Shared/Store.Shared.DTOs/Store.Shared.DTOs.csproj", "Shared/Store.Shared.DTOs/"]
COPY ["Shared/Store.Shared.Contracts/Store.Shared.Contracts.csproj", "Shared/Store.Shared.Contracts/"]

# Copy service project
COPY ["Services/ProductService/Store.ProductService.csproj", "Services/ProductService/"]

# Restore dependencies
RUN dotnet restore "Services/ProductService/Store.ProductService.csproj"

# Copy source code
COPY ["Services/ProductService/", "Services/ProductService/"]
COPY ["Shared/", "Shared/"]

# Build the application
WORKDIR "/src/Services/ProductService"
RUN dotnet build "Store.ProductService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Store.ProductService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Store.ProductService.dll"]
