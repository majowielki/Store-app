@echo off

echo Applying database migrations for all services...

echo Migrating Identity Service...
cd ..\..\Services\IdentityService
dotnet ef database update --verbose
cd ..\..\Infrastructure\scripts

echo Migrating Product Service...
cd ..\..\Services\ProductService
dotnet ef database update --verbose
cd ..\..\Infrastructure\scripts

echo Migrating Cart Service...
cd ..\..\Services\CartService
dotnet ef database update --verbose
cd ..\..\Infrastructure\scripts

echo Migrating Order Service...
cd ..\..\Services\OrderService
dotnet ef database update --verbose
cd ..\..\Infrastructure\scripts

echo All database migrations completed!
pause
