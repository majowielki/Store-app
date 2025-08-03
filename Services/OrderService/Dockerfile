# Multi-stage Dockerfile for Order Service
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5004
ENV ASPNETCORE_URLS=http://+:5004

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared projects first for better layer caching
COPY ["Shared/Store.Shared/Store.Shared.csproj", "Shared/Store.Shared/"]

# Copy service project
COPY ["Services/OrderService/Store.OrderService.csproj", "Services/OrderService/"]

# Restore dependencies
RUN dotnet restore "Services/OrderService/Store.OrderService.csproj"

# Copy source code
COPY ["Services/OrderService/", "Services/OrderService/"]
COPY ["Shared/", "Shared/"]

# Build the application
WORKDIR "/src/Services/OrderService"
RUN dotnet build "Store.OrderService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Store.OrderService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create a non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "Store.OrderService.dll"]
