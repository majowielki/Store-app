# Simple Store App Docker Management Script
param([string]$Action)

Write-Host " Store App Docker Management" -ForegroundColor Green

# Change to Infrastructure directory
$currentDir = Get-Location
$infraDir = Split-Path -Parent $PSScriptRoot
Set-Location $infraDir

Write-Host "Working in: $infraDir" -ForegroundColor Gray

switch ($Action.ToLower()) {
    "build" {
        Write-Host " Building and starting services..." -ForegroundColor Yellow
        docker-compose up --build -d
        if ($LASTEXITCODE -eq 0) {
            Write-Host " Services started!" -ForegroundColor Green
            Write-Host " API Gateway: http://localhost:5000" -ForegroundColor Cyan
            Write-Host " Frontend: http://localhost:3000" -ForegroundColor Cyan
        }
    }
    "start" {
        Write-Host " Starting services..." -ForegroundColor Yellow
        docker-compose up -d
    }
    "stop" {
        Write-Host " Stopping services..." -ForegroundColor Yellow
        docker-compose down
    }
    "logs" {
        Write-Host " Showing logs..." -ForegroundColor Yellow
        docker-compose logs -f
    }
    "clean" {
        Write-Host " Cleaning up..." -ForegroundColor Yellow
        docker-compose down --remove-orphans
        docker image prune -f
    }
    "status" {
        Write-Host " Service status:" -ForegroundColor Yellow
        docker-compose ps
    }
    default {
        Write-Host "Usage: .\simple-docker.ps1 [build|start|stop|logs|clean|status]" -ForegroundColor White
        Write-Host "Examples:" -ForegroundColor Gray
        Write-Host "  .\simple-docker.ps1 build   # Build and start all services" -ForegroundColor Gray
        Write-Host "  .\simple-docker.ps1 logs    # View service logs" -ForegroundColor Gray
        Write-Host "  .\simple-docker.ps1 stop    # Stop all services" -ForegroundColor Gray
        Write-Host "  .\simple-docker.ps1 clean   # Clean up everything" -ForegroundColor Gray
    }
}

Set-Location $currentDir
