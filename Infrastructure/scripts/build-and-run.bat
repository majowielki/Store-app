@echo off
echo 🚀 Building Store App Docker Images...
echo.

cd /d "%~dp0"

echo 📦 Building Identity Service...
docker build -t store-identity-service ../Services/IdentityService
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Failed to build Identity Service
    pause
    exit /b 1
)

echo 📦 Building Product Service...
docker build -t store-product-service ../Services/ProductService
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Failed to build Product Service
    pause
    exit /b 1
)

echo 📦 Building Cart Service...
docker build -t store-cart-service ../Services/CartService
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Failed to build Cart Service
    pause
    exit /b 1
)

echo 📦 Building Order Service...
docker build -t store-order-service ../Services/OrderService
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Failed to build Order Service
    pause
    exit /b 1
)

echo 📦 Building Audit Service...
docker build -t store-audit-service ../Services/AuditLogService
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Failed to build Audit Service
    pause
    exit /b 1
)

echo 📦 Building Gateway Service...
docker build -t store-gateway-service ../Gateway/APIGateway
if %ERRORLEVEL% NEQ 0 (
    echo ❌ Failed to build Gateway Service
    pause
    exit /b 1
)

echo.
echo ✅ All services built successfully!
echo.
echo 🚀 Starting services with Docker Compose...
docker-compose up -d

if %ERRORLEVEL% NEQ 0 (
    echo ❌ Failed to start services
    pause
    exit /b 1
)

echo.
echo ✅ Store App is starting up!
echo.
echo 🌐 Access points:
echo   Frontend:        http://localhost:3000
echo   API Gateway:     http://localhost:5000
echo   Identity Service: http://localhost:5001
echo   Product Service:  http://localhost:5002
echo   Cart Service:     http://localhost:5003
echo   Order Service:    http://localhost:5004
echo   Audit Service:    http://localhost:5005
echo.
echo 📊 Database:
echo   PostgreSQL:      localhost:5432
echo   Redis:           localhost:6379
echo.
echo 📋 Useful commands:
echo   View logs:       docker-compose logs -f
echo   Stop services:   docker-compose down
echo   Restart:         docker-compose restart
echo.

pause
