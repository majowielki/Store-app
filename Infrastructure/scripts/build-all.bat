@echo off

echo Building all microservices...

:: Build Shared libraries
echo Building Shared libraries...
cd ..\..\Shared\Store.Shared.DTOs
dotnet build
cd ..\Store.Shared.Contracts
dotnet build
cd ..\Store.Shared
dotnet build
cd ..\..\Infrastructure\scripts

:: Build microservices
set services=ProductService IdentityService CartService OrderService

for %%s in (%services%) do (
    echo Building %%s...
    cd ..\..\Services\%%s
    dotnet build
    cd ..\..\Infrastructure\scripts
)

:: Build Gateway
echo Building Gateway...
cd ..\..\Gateway\APIGateway
dotnet build
cd ..\..\Infrastructure\scripts

echo All microservices built successfully!
pause
