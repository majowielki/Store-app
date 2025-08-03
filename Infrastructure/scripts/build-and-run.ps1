# PowerShell script to build and run Store App with Docker Compose
param(
    [switch]$Build = $false,
    [switch]$Clean = $false,
    [switch]$Logs = $false,
    [switch]$Stop = $false
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$InfraDir = Split-Path -Parent $ScriptDir

Write-Host " Store App Docker Management Script" -ForegroundColor Green
Write-Host "Working directory: $InfraDir" -ForegroundColor Gray
Write-Host ""

# Change to Infrastructure directory
Set-Location $InfraDir

try {
    if ($Clean) {
        Write-Host " Cleaning up Docker resources..." -ForegroundColor Yellow
        
        # Stop and remove containers
        docker-compose down --remove-orphans
        
        # Remove unused images
        docker image prune -f
        
        Write-Host " Cleanup completed!" -ForegroundColor Green
        return
    }

    if ($Stop) {
        Write-Host " Stopping Store App services..." -ForegroundColor Yellow
        docker-compose down
        Write-Host " Services stopped!" -ForegroundColor Green
        return
    }

    if ($Logs) {
        Write-Host " Showing service logs..." -ForegroundColor Yellow
        docker-compose logs -f
        return
    }

    if ($Build) {
        Write-Host " Building Docker images..." -ForegroundColor Yellow
        
        $services = @(
            @{Name="Identity Service"; Path="../Services/IdentityService"; Tag="store-identity-service"},
            @{Name="Product Service"; Path="../Services/ProductService"; Tag="store-product-service"},
            @{Name="Cart Service"; Path="../Services/CartService"; Tag="store-cart-service"},
            @{Name="Order Service"; Path="../Services/OrderService"; Tag="store-order-service"},
            @{Name="Audit Service"; Path="../Services/AuditLogService"; Tag="store-audit-service"},
            @{Name="Gateway Service"; Path="../Gateway/APIGateway"; Tag="store-gateway-service"}
        )

        foreach ($service in $services) {
            Write-Host " Building $($service.Name)..." -ForegroundColor Cyan
            docker build -t $service.Tag $service.Path
            
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to build $($service.Name)"
            }
            
            Write-Host " $($service.Name) built successfully!" -ForegroundColor Green
        }
        
        Write-Host ""
        Write-Host " All services built successfully!" -ForegroundColor Green
    }

    # Start services
    Write-Host " Starting Store App services..." -ForegroundColor Yellow
    
    if ($Build) {
        docker-compose up --build -d
    } else {
        docker-compose up -d
    }
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start services"
    }

    # Wait a moment for services to start
    Write-Host " Waiting for services to start..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5

    # Check service status
    Write-Host " Checking service status..." -ForegroundColor Yellow
    docker-compose ps

    Write-Host ""
    Write-Host " Store App is running!" -ForegroundColor Green
    Write-Host ""
    Write-Host " Access Points:" -ForegroundColor Magenta
    Write-Host "   Frontend:         http://localhost:3000" -ForegroundColor White
    Write-Host "   API Gateway:      http://localhost:5000" -ForegroundColor White
    Write-Host "   Identity Service: http://localhost:5001" -ForegroundColor White
    Write-Host "   Product Service:  http://localhost:5002" -ForegroundColor White
    Write-Host "   Cart Service:     http://localhost:5003" -ForegroundColor White
    Write-Host "   Order Service:    http://localhost:5004" -ForegroundColor White
    Write-Host "   Audit Service:    http://localhost:5005" -ForegroundColor White
    Write-Host ""
    Write-Host " Databases:" -ForegroundColor Magenta
    Write-Host "   PostgreSQL:       localhost:5432" -ForegroundColor White
    Write-Host "   Redis:            localhost:6379" -ForegroundColor White
    Write-Host ""
    Write-Host " Useful Commands:" -ForegroundColor Magenta
    Write-Host "   View logs:        .\build-and-run.ps1 -Logs" -ForegroundColor White
    Write-Host "   Stop services:    .\build-and-run.ps1 -Stop" -ForegroundColor White
    Write-Host "   Clean up:         .\build-and-run.ps1 -Clean" -ForegroundColor White
    Write-Host "   Rebuild:          .\build-and-run.ps1 -Build" -ForegroundColor White
    Write-Host ""

    # Health check
    Write-Host " Performing health checks..." -ForegroundColor Yellow
    $healthChecks = @(
        @{Name="API Gateway"; Url="http://localhost:5000/health"},
        @{Name="Identity Service"; Url="http://localhost:5001/health"},
        @{Name="Product Service"; Url="http://localhost:5002/health"},
        @{Name="Cart Service"; Url="http://localhost:5003/health"},
        @{Name="Order Service"; Url="http://localhost:5004/health"},
        @{Name="Audit Service"; Url="http://localhost:5005/health"}
    )

    Start-Sleep -Seconds 10  # Wait for services to be ready

    foreach ($check in $healthChecks) {
        try {
            $response = Invoke-WebRequest -Uri $check.Url -TimeoutSec 5 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host "    $($check.Name)" -ForegroundColor Green
            } else {
                Write-Host "    $($check.Name) - Status: $($response.StatusCode)" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "    $($check.Name) - Not responding" -ForegroundColor Red
        }
    }

} catch {
    Write-Host " Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host " Try running with -Clean to reset everything" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host " Setup completed! Your Store App is ready to use." -ForegroundColor Green
