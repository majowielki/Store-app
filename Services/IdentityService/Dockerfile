FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Store.IdentityService.csproj", "./"]
COPY ["../../Shared/Store.Shared/Store.Shared.csproj", "../../Shared/Store.Shared/"]
COPY ["../../Shared/Store.Shared.DTOs/Store.Shared.DTOs.csproj", "../../Shared/Store.Shared.DTOs/"]
COPY ["../../Shared/Store.Shared.Contracts/Store.Shared.Contracts.csproj", "../../Shared/Store.Shared.Contracts/"]
RUN dotnet restore "./Store.IdentityService.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "Store.IdentityService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Store.IdentityService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Store.IdentityService.dll"]
