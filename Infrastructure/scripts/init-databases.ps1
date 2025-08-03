# PowerShell script to initialize PostgreSQL databases for Store App
# This script can be run from Windows PowerShell

Write-Host "🚀 Initializing Store App databases..." -ForegroundColor Green

# Database configuration
$postgresUser = "store_user"
$postgresPassword = "StrongPassword123!"
$postgresHost = "localhost"
$postgresPort = "5432"
$postgresDb = "store_db"

# Array of databases to create
$databases = @(
    @{Name="store_identity_db"; Description="Identity Service"},
    @{Name="store_product_db"; Description="Product Service"},
    @{Name="store_cart_db"; Description="Cart Service"},
    @{Name="store_order_db"; Description="Order Service"},
    @{Name="store_audit_db"; Description="Audit Log Service"}
)

try {
    Write-Host "📡 Connecting to PostgreSQL server..." -ForegroundColor Yellow
    
    # SQL commands to execute
    $sqlCommands = @"
-- Create databases for each microservice
CREATE DATABASE store_identity_db;
CREATE DATABASE store_product_db;
CREATE DATABASE store_cart_db;
CREATE DATABASE store_order_db;
CREATE DATABASE store_audit_db;

-- Grant privileges to the user for all databases
GRANT ALL PRIVILEGES ON DATABASE store_identity_db TO $postgresUser;
GRANT ALL PRIVILEGES ON DATABASE store_product_db TO $postgresUser;
GRANT ALL PRIVILEGES ON DATABASE store_cart_db TO $postgresUser;
GRANT ALL PRIVILEGES ON DATABASE store_order_db TO $postgresUser;
GRANT ALL PRIVILEGES ON DATABASE store_audit_db TO $postgresUser;
"@

    # Execute SQL commands using Docker
    Write-Host "🔧 Creating databases..." -ForegroundColor Yellow
    
    $dockerCommand = "docker exec -i store-postgres psql -U $postgresUser -d $postgresDb"
    $sqlCommands | & docker exec -i store-postgres psql -U $postgresUser -d $postgresDb
    
    Write-Host "✅ Database initialization completed successfully!" -ForegroundColor Green
    Write-Host "📊 Created databases:" -ForegroundColor Cyan
    
    foreach($db in $databases) {
        Write-Host "   - $($db.Name) ($($db.Description))" -ForegroundColor White
    }
    
    Write-Host "`n🎯 Next steps:" -ForegroundColor Magenta
    Write-Host "   1. Update your appsettings.json files with the correct connection strings" -ForegroundColor White
    Write-Host "   2. Run your microservices to apply Entity Framework migrations" -ForegroundColor White
    Write-Host "   3. Test the database connections" -ForegroundColor White
    
} catch {
    Write-Host "❌ Error initializing databases: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "💡 Make sure PostgreSQL container is running: docker-compose up postgres" -ForegroundColor Yellow
    exit 1
}

Write-Host "`nPress any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
